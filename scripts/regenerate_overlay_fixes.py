from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "mod" / "StardewLivingRPG" / "assets"
BASE_PATH = ASSETS / "town-square-magician-rounds.json"
LOCALES = ("es", "pt-br", "ja", "ko", "de", "ru", "fr", "zh-cn", "it", "tr")
PROPER_NAME_ENGLISH = {
    "abigail",
    "alex",
    "birdie",
    "caroline",
    "clint",
    "demetrius",
    "elliott",
    "emily",
    "evelyn",
    "fizz",
    "george",
    "gil",
    "gunther",
    "gus",
    "haley",
    "harvey",
    "jas",
    "jodi",
    "kent",
    "krobus",
    "leah",
    "leo",
    "lewis",
    "linus",
    "marlon",
    "marnie",
    "maru",
    "mister qi",
    "morris",
    "pam",
    "penny",
    "pierre",
    "robin",
    "sam",
    "sandy",
    "sebastian",
    "shane",
    "vincent",
    "welwick",
    "willy",
    "bongo",
}
ASCII_WORD = re.compile(r"[A-Za-z]")


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def get_visible_answer(entry: dict) -> str:
    reveal = (entry.get("RevealAnswer") or "").strip()
    if reveal:
        return reveal

    for answer in entry.get("Answers") or []:
        if answer and answer.strip():
            return answer.strip()

    return ""


def first_visible_character(value: str) -> str:
    for character in value.strip():
        if character.isspace():
            continue
        if character in "\"'“”‘’«»「」『』()[]{}<>":
            continue
        return character

    return ""


def build_structural_clue(locale: str, base_clue: str, visible_answer: str) -> str | None:
    if not base_clue or not visible_answer:
        return None

    is_first_word = base_clue.startswith("The first word starts with ")
    is_whole_answer = base_clue.startswith("It starts with ")
    if not is_first_word and not is_whole_answer:
        return None

    first_char = first_visible_character(visible_answer)
    if not first_char:
        return None

    return {
        "es": (
            f"La primera palabra empieza con {first_char}."
            if is_first_word
            else f"Empieza con {first_char}."
        ),
        "pt-br": (
            f"A primeira palavra come\u00e7a com {first_char}."
            if is_first_word
            else f"Come\u00e7a com {first_char}."
        ),
        "ja": f"\u300c{first_char}\u300d\u3067\u59cb\u307e\u308b\u3002",
        "ko": f"'{first_char}'\ub85c \uc2dc\uc791\ud55c\ub2e4.",
        "de": (
            f"Das erste Wort beginnt mit {first_char}."
            if is_first_word
            else f"Es beginnt mit {first_char}."
        ),
        "ru": (
            f"\u041f\u0435\u0440\u0432\u043e\u0435 \u0441\u043b\u043e\u0432\u043e \u043d\u0430\u0447\u0438\u043d\u0430\u0435\u0442\u0441\u044f \u0441 {first_char}."
            if is_first_word
            else f"\u041d\u0430\u0447\u0438\u043d\u0430\u0435\u0442\u0441\u044f \u0441 {first_char}."
        ),
        "fr": (
            f"Le premier mot commence par {first_char}."
            if is_first_word
            else f"Cela commence par {first_char}."
        ),
        "zh-cn": f"\u5b83\u4ee5\u201c{first_char}\u201d\u5f00\u5934\u3002",
        "it": (
            f"La prima parola inizia con {first_char}."
            if is_first_word
            else f"Inizia con {first_char}."
        ),
        "tr": (
            f"\u0130lk kelime {first_char} ile ba\u015flar."
            if is_first_word
            else f"{first_char} ile ba\u015flar."
        ),
    }.get(locale)


def is_mojibake(text: str) -> bool:
    return "Ã" in text or "\ufffd" in text or "??" in text


def is_nonproper_english_answer(locale: str, round_id: str, answer: str, base_answer: str) -> bool:
    if not answer or answer.isdigit() or answer != base_answer:
        return False
    if not ASCII_WORD.search(answer):
        return False
    if answer.lower() in PROPER_NAME_ENGLISH:
        return False
    if locale == "tr" and round_id == "word_0146":
        return True
    return locale in {"ja", "ko", "ru", "zh-cn"}


def repair_locale(locale: str, write_changes: bool) -> tuple[int, list[str]]:
    base_catalog = load_json(BASE_PATH)
    base_rounds = {entry["Id"]: entry for entry in base_catalog["Rounds"]}
    path = ASSETS / f"town-square-magician-rounds.{locale}.json"
    payload = load_json(path)
    rounds = payload["Rounds"]
    changed = 0
    issues: list[str] = []

    for round_id, entry in rounds.items():
        base_entry = base_rounds.get(round_id)
        if not base_entry:
            continue

        clues = list(entry.get("Clues") or [])
        visible_answer = get_visible_answer(entry)
        for i in range(min(len(clues), len(base_entry.get("Clues") or []))):
            expected = build_structural_clue(locale, base_entry["Clues"][i], visible_answer)
            if expected and clues[i] != expected:
                clues[i] = expected
                changed += 1
        entry["Clues"] = clues

        for text in [visible_answer, *clues]:
            if text and is_mojibake(text):
                issues.append(f"{locale}:{round_id}: mojibake -> {text!r}")

        if is_nonproper_english_answer(locale, round_id, visible_answer, get_visible_answer(base_entry)):
            issues.append(f"{locale}:{round_id}: untranslated answer -> {visible_answer!r}")

    if write_changes and changed:
        path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    return changed, issues


def main() -> int:
    parser = argparse.ArgumentParser(description="Repair magician overlay structural clues and scan for regressions.")
    parser.add_argument("--check", action="store_true", help="Scan only; do not rewrite files.")
    args = parser.parse_args()

    total_changes = 0
    all_issues: list[str] = []
    for locale in LOCALES:
        changes, issues = repair_locale(locale, write_changes=not args.check)
        total_changes += changes
        all_issues.extend(issues)
        print(f"{locale}: structural clue updates={changes}, issues={len(issues)}")

    if all_issues:
        print("\nIssues:")
        for issue in all_issues:
            print(issue)
        return 1

    if args.check:
        print("\nOverlay scan passed.")
    else:
        print(f"\nOverlay repair complete. Structural clue updates: {total_changes}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
