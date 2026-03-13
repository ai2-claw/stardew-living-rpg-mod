import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "mod" / "StardewLivingRPG" / "assets"


QUEST_LOCALES = {
    "ja": {
        "TitleVariants": {
            "gather_crop": ["{{target}}集め", "収穫の手助け: {{target}}", "畑の用事: {{target}}"],
            "deliver_item": ["物資の届け物: {{target}}", "市場納品: {{target}}", "町への配達: {{target}}"],
            "mine_resource": ["採掘の用事: {{target}}", "採掘依頼: {{target}}", "鉱石の頼み: {{target}}"],
            "social_visit": ["{{target}}を訪ねる", "{{target}}に声をかける", "近所の気づかい: {{target}}"],
            "default": ["町の頼み: {{target}}", "共同の用事: {{target}}", "町長の頼み: {{target}}"],
        },
        "SummaryTemplates": {
            "gather_crop": "{{issuer}}は町の棚のために{{count}}個の{{target}}を欲しがっている。",
            "deliver_item": "{{issuer}}は町の商いのために{{count}}個の{{target}}を求めている。",
            "mine_resource": "{{issuer}}は骨の折れる仕事のために鉱山の{{count}}個の{{target}}を必要としている。",
            "social_visit": "{{issuer}}は{{target}}の様子を見に行ってほしいと言っている。",
            "default": "{{issuer}}が{{count}}個の{{target}}を求める町の頼みを出した。",
        },
        "Messages": {
            "accepted": "依頼を受けた: {{title}}",
            "active_not_found": "進行中の依頼が見つからない: {{questId}}",
            "visit_first": "先に{{target}}を訪ねてから、この依頼を終えて。",
            "not_ready": "まだ完了できない: {{title}}。",
            "need_items": "{{need}}個の{{item}}が必要だが、あるのは{{have}}個。",
            "completed": "依頼完了: {{title}} (+{{reward}}g{{consumed}})",
            "completed_consumed_part": "、{{count}}個の{{item}}を渡した",
            "progress_ready": "完了できる: {{title}}",
            "progress_items": "未達成: {{have}}/{{need}}個の{{item}}。",
            "progress_visit": "未達成: 先に{{target}}を訪ねて。",
            "progress_items_bar": "進行: {{have}}/{{need}} {{item}}",
            "progress_visit_bar": "訪問: {{have}}/{{need}} {{target}}",
        },
    },
    "ko": {
        "TitleVariants": {
            "gather_crop": ["{{target}} 모으기", "수확 도우미: {{target}}", "밭일 심부름: {{target}}"],
            "deliver_item": ["물자 전달: {{target}}", "시장 납품: {{target}}", "마을 배달: {{target}}"],
            "mine_resource": ["광산 심부름: {{target}}", "채굴 요청: {{target}}", "광석 부탁: {{target}}"],
            "social_visit": ["{{target}} 안부 보기", "{{target}} 찾아가기", "이웃 심부름: {{target}}"],
            "default": ["마을 부탁: {{target}}", "공동 일감: {{target}}", "시장님의 부탁: {{target}}"],
        },
        "SummaryTemplates": {
            "gather_crop": "{{issuer}}이(가) 마을 진열대를 위해 {{target}} {{count}}개를 구하고 있다.",
            "deliver_item": "{{issuer}}이(가) 마을 장사를 위해 {{target}} {{count}}개를 찾고 있다.",
            "mine_resource": "{{issuer}}이(가) 거친 일을 버티려면 광산의 {{target}} {{count}}개가 필요하다.",
            "social_visit": "{{issuer}}이(가) {{target}}에게 들러 안부를 확인해 달라고 했다.",
            "default": "{{issuer}}이(가) {{target}} {{count}}개를 구하는 마을 부탁을 올렸다.",
        },
        "Messages": {
            "accepted": "의뢰 수락: {{title}}",
            "active_not_found": "진행 중 의뢰를 찾지 못함: {{questId}}",
            "visit_first": "먼저 {{target}}에게 들른 뒤 이 의뢰를 마쳐라.",
            "not_ready": "아직 준비되지 않음: {{title}}.",
            "need_items": "{{item}} {{need}}개가 필요하지만 지금은 {{have}}개뿐이다.",
            "completed": "의뢰 완료: {{title}} (+{{reward}}g{{consumed}})",
            "completed_consumed_part": ", {{item}} {{count}}개 전달",
            "progress_ready": "완료 가능: {{title}}",
            "progress_items": "아직 아님: {{have}}/{{need}} {{item}} 전달됨.",
            "progress_visit": "아직 아님: 먼저 {{target}}에게 가라.",
            "progress_items_bar": "진행: {{have}}/{{need}} {{item}}",
            "progress_visit_bar": "방문: {{have}}/{{need}} {{target}}",
        },
    },
    "ru": {
        "TitleVariants": {
            "gather_crop": ["Собрать {{target}}", "Помощь с урожаем: {{target}}", "Полевое дело: {{target}}"],
            "deliver_item": ["Поставка: {{target}}", "Доставка на рынок: {{target}}", "Доставка в город: {{target}}"],
            "mine_resource": ["Поход в шахту: {{target}}", "Зов шахтёра: {{target}}", "Заказ руды: {{target}}"],
            "social_visit": ["Дружеский визит: {{target}}", "Зайти к {{target}}", "Соседское дело: {{target}}"],
            "default": ["Городская просьба: {{target}}", "Общее дело: {{target}}", "Просьба мэра: {{target}}"],
        },
        "SummaryTemplates": {
            "gather_crop": "{{issuer}} говорит, что городу нужны {{count}} {{target}}, чтобы полки не пустовали.",
            "deliver_item": "{{issuer}} говорит, что городу нужны {{count}} {{target}}, чтобы торговля не замирала.",
            "mine_resource": "{{issuer}} нужны {{count}} {{target}} из шахты для тяжёлой работы.",
            "social_visit": "{{issuer}} просит заглянуть к {{target}} и убедиться, что у него всё в порядке.",
            "default": "{{issuer}} разместил городскую просьбу на {{count}} {{target}}.",
        },
        "Messages": {
            "accepted": "Просьба принята: {{title}}",
            "active_not_found": "Активная просьба не найдена: {{questId}}",
            "visit_first": "Сначала навести {{target}}, потом заверши эту просьбу.",
            "not_ready": "Просьба ещё не готова: {{title}}.",
            "need_items": "Нужно {{need}} {{item}}, а есть только {{have}}.",
            "completed": "Просьба выполнена: {{title}} (+{{reward}}g{{consumed}})",
            "completed_consumed_part": ", отдано {{count}} {{item}}",
            "progress_ready": "Можно завершить: {{title}}",
            "progress_items": "Пока нет: доставлено {{have}}/{{need}} {{item}}.",
            "progress_visit": "Пока нет: сначала навести {{target}}.",
            "progress_items_bar": "Ход: {{have}}/{{need}} {{item}}",
            "progress_visit_bar": "Визит: {{have}}/{{need}} {{target}}",
        },
    },
    "zh-cn": {
        "TitleVariants": {
            "gather_crop": ["收集{{target}}", "收成帮手：{{target}}", "田间跑腿：{{target}}"],
            "deliver_item": ["补给送达：{{target}}", "市场交货：{{target}}", "小镇配送：{{target}}"],
            "mine_resource": ["矿洞跑腿：{{target}}", "矿工委托：{{target}}", "矿石请求：{{target}}"],
            "social_visit": ["友好探访：{{target}}", "去看看{{target}}", "邻里跑腿：{{target}}"],
            "default": ["小镇委托：{{target}}", "社区任务：{{target}}", "镇长委托：{{target}}"],
        },
        "SummaryTemplates": {
            "gather_crop": "{{issuer}}说小镇需要{{count}}个{{target}}，这样货架才不会空。",
            "deliver_item": "{{issuer}}说小镇需要{{count}}个{{target}}，这样本地生意才能继续。",
            "mine_resource": "{{issuer}}需要从矿洞带回{{count}}个{{target}}，好让粗活继续。",
            "social_visit": "{{issuer}}想请人去看看{{target}}，确认对方一切安好。",
            "default": "{{issuer}}张贴了一份需要{{count}}个{{target}}的小镇委托。",
        },
        "Messages": {
            "accepted": "已接受委托：{{title}}",
            "active_not_found": "未找到进行中的委托：{{questId}}",
            "visit_first": "先去见{{target}}，再完成这份委托。",
            "not_ready": "委托尚未完成：{{title}}。",
            "need_items": "需要{{need}}个{{item}}，你现在只有{{have}}个。",
            "completed": "委托完成：{{title}} (+{{reward}}g{{consumed}})",
            "completed_consumed_part": "，交出了{{count}}个{{item}}",
            "progress_ready": "可以完成：{{title}}",
            "progress_items": "还没完成：已交付{{have}}/{{need}}个{{item}}。",
            "progress_visit": "还没完成：先去见{{target}}。",
            "progress_items_bar": "进度：{{have}}/{{need}} {{item}}",
            "progress_visit_bar": "探访：{{have}}/{{need}} {{target}}",
        },
    },
}


PROMPT_MAP = {
    "ja": {
        "Guess the hidden word.": "隠れた言葉を当てて。",
        "Guess these two words.": "この二語を当てて。",
        "Guess this phrase.": "この言葉を当てて。",
        "Guess the number.": "数字を当てて。",
    },
    "ko": {
        "Guess the hidden word.": "숨은 단어를 맞혀라.",
        "Guess these two words.": "이 두 단어를 맞혀라.",
        "Guess this phrase.": "이 구절을 맞혀라.",
        "Guess the number.": "숫자를 맞혀라.",
    },
    "ru": {
        "Guess the hidden word.": "Угадай слово.",
        "Guess these two words.": "Угадай эти два слова.",
        "Guess this phrase.": "Угадай эту фразу.",
        "Guess the number.": "Угадай число.",
    },
}


OPENING_MAP = {
    "ja": {
        "A quick number today.": "今日は手早い数字だ。",
        "A neat number today.": "今日はきれいな数字だ。",
        "A small riddle today.": "今日は小さな謎だ。",
        "Count sharp for this one.": "これは慎重に数えな。",
        "A town word today.": "今日は町の言葉だ。",
        "Two town words today.": "今日は町の二語だ。",
        "A town phrase today.": "今日は町の言い回しだ。",
    },
    "ko": {
        "A quick number today.": "오늘은 빠른 숫자다.",
        "A neat number today.": "오늘은 깔끔한 숫자다.",
        "A small riddle today.": "오늘은 작은 수수께끼다.",
        "Count sharp for this one.": "이번 건 잘 세어라.",
        "A town word today.": "오늘은 마을 단어다.",
        "Two town words today.": "오늘은 마을 두 단어다.",
        "A town phrase today.": "오늘은 마을 구절이다.",
    },
    "ru": {
        "A quick number today.": "Сегодня быстрое число.",
        "A neat number today.": "Сегодня ладное число.",
        "A small riddle today.": "Сегодня маленькая загадка.",
        "Count sharp for this one.": "Тут считай внимательно.",
        "A town word today.": "Сегодня городское слово.",
        "Two town words today.": "Сегодня два городских слова.",
        "A town phrase today.": "Сегодня городская фраза.",
    },
}


VICTORY_MAP = {
    "ja": {
        "You caught it cleanly.": "きれいに当てたな。",
        "That was the number.": "その数字で正解だ。",
        "Sharp eye. That was it.": "目が利くな。それだ。",
        "You pulled it from smoke.": "煙の中から引いたな。",
        "You named it.": "言い当てたな。",
    },
    "ko": {
        "You caught it cleanly.": "깔끔하게 맞혔다.",
        "That was the number.": "바로 그 숫자다.",
        "Sharp eye. That was it.": "눈썰미가 좋군. 그거다.",
        "You pulled it from smoke.": "연기 속에서 건져냈군.",
        "You named it.": "정확히 말했다.",
    },
    "ru": {
        "You caught it cleanly.": "Чисто угадал.",
        "That was the number.": "Это и было число.",
        "Sharp eye. That was it.": "Глаз острый. Это оно.",
        "You pulled it from smoke.": "Вытащил его из дыма.",
        "You named it.": "Ты назвал верно.",
    },
}


SEMANTIC_CLUES = {
    "A machine can make this.": {"ja": "機械で作れる。", "ko": "기계로 만들 수 있다.", "ru": "Это делает машина."},
    "A machine helps make it.": {"ja": "機械が役に立つ。", "ko": "기계가 만드는 데 도움이 된다.", "ru": "Машина помогает это сделать."},
    "A strange aura surrounds it.": {"ja": "妙な気配がある。", "ko": "이상한 기운이 감돈다.", "ru": "Его окружает странная аура."},
    "An animal can give this.": {"ja": "動物がくれる。", "ko": "동물이 내준다.", "ru": "Это может дать животное."},
    "Gunther would take it.": {"ja": "ガンターが喜ぶ。", "ko": "건터가 받아 줄 것이다.", "ru": "Гюнтер бы это принял."},
    "It belongs by the water.": {"ja": "水辺が似合う。", "ko": "물가에 어울린다.", "ru": "Ему место у воды."},
    "It belongs in a fight.": {"ja": "戦い向きだ。", "ko": "전투에 어울린다.", "ru": "Это для боя."},
    "It belongs in a room.": {"ja": "部屋に置くものだ。", "ko": "방에 두는 물건이다.", "ru": "Ему место в комнате."},
    "It belongs inside the house.": {"ja": "家の中に置く。", "ko": "집 안에 두는 것이다.", "ru": "Ему место в доме."},
    "It belongs near water.": {"ja": "水辺で使う。", "ko": "물가에서 쓰인다.", "ru": "Ему место у воды."},
    "It belongs on a farm.": {"ja": "農場向きだ。", "ko": "농장에 어울린다.", "ru": "Это для фермы."},
    "It belongs out in a field.": {"ja": "畑に似合う。", "ko": "밭에 어울린다.", "ru": "Ему место в поле."},
    "It carries a strange vibe.": {"ja": "妙な気配をまとう。", "ko": "이상한 분위기를 띤다.", "ru": "От него веет странностью."},
    "It comes from a kitchen.": {"ja": "台所から生まれる。", "ko": "부엌에서 나온다.", "ru": "Это приходит с кухни."},
    "It comes from a tree.": {"ja": "木から取れる。", "ko": "나무에서 나온다.", "ru": "Это берут с дерева."},
    "It comes from an animal.": {"ja": "動物から取れる。", "ko": "동물에게서 나온다.", "ru": "Это дают животные."},
    "It comes from stone.": {"ja": "石から出る。", "ko": "돌에서 나온다.", "ru": "Это выходит из камня."},
    "It comes from the sea.": {"ja": "海から来る。", "ko": "바다에서 온다.", "ru": "Это приходит из моря."},
    "It cuts wood.": {"ja": "木を切る。", "ko": "나무를 벤다.", "ru": "Им рубят дерево."},
    "It feels a little magical.": {"ja": "少し魔法めいている。", "ko": "조금 마법 같다.", "ru": "В нём есть капля магии."},
    "It feels magical.": {"ja": "魔法めいている。", "ko": "마법처럼 느껴진다.", "ru": "В нём чувствуется магия."},
    "It fires from afar.": {"ja": "遠くから撃てる。", "ko": "멀리서 쏜다.", "ru": "Бьёт издалека."},
    "It fits a festival mood.": {"ja": "祭り向きだ。", "ko": "축제 분위기에 어울린다.", "ru": "Подходит для праздника."},
    "It fits farm work.": {"ja": "農作業向きだ。", "ko": "농사일에 어울린다.", "ru": "Подходит для фермерской работы."},
    "It gives off light.": {"ja": "明かりを放つ。", "ko": "빛을 낸다.", "ru": "От него идёт свет."},
    "It handles rough weather.": {"ja": "荒天に強い。", "ko": "거친 날씨를 버틴다.", "ru": "Выдерживает суровую погоду."},
    "It hatches or grows life.": {"ja": "命を育てる。", "ko": "생명을 키우거나 부화시킨다.", "ru": "В нём растят или выводят жизнь."},
    "It helps from your hand.": {"ja": "手元で役立つ。", "ko": "손에서 힘을 보탠다.", "ru": "Помогает прямо в руке."},
    "It helps with barn work.": {"ja": "家畜小屋で役立つ。", "ko": "축사 일에 도움이 된다.", "ru": "Помогает в работе с хлевом."},
    "It helps with fishing.": {"ja": "釣りの助けになる。", "ko": "낚시에 도움이 된다.", "ru": "Помогает с рыбалкой."},
    "It hits hard in combat.": {"ja": "戦いで重く効く。", "ko": "전투에서 묵직하게 친다.", "ru": "В бою бьёт тяжело."},
    "It is a bright gem.": {"ja": "輝く宝石だ。", "ko": "빛나는 보석이다.", "ru": "Это яркий самоцвет."},
    "It is a cooked dish.": {"ja": "料理だ。", "ko": "요리다.", "ru": "Это готовое блюдо."},
    "It is a crafting material.": {"ja": "素材だ。", "ko": "제작 재료다.", "ru": "Это ремесленный материал."},
    "It is a drink.": {"ja": "飲み物だ。", "ko": "음료다.", "ru": "Это напиток."},
    "It is a farm flower.": {"ja": "農場の花だ。", "ko": "농장의 꽃이다.", "ru": "Это фермерский цветок."},
    "It is a fish.": {"ja": "魚だ。", "ko": "물고기다.", "ru": "Это рыба."},
    "It is a quick blade.": {"ja": "素早い刃だ。", "ko": "빠른 칼날이다.", "ru": "Это быстрый клинок."},
    "It is a sea catch.": {"ja": "海の獲物だ。", "ko": "바다에서 잡는다.", "ru": "Это морской улов."},
    "It is fancy headwear.": {"ja": "しゃれた帽子だ。", "ko": "멋을 낸 머리장식이다.", "ru": "Это нарядный головной убор."},
    "It is playful headwear.": {"ja": "遊び心のある帽子だ。", "ko": "장난기 있는 머리장식이다.", "ru": "Это игривый головной убор."},
    "It is sturdy footwear.": {"ja": "丈夫な履き物だ。", "ko": "튼튼한 신발이다.", "ru": "Это крепкая обувь."},
    "It is tied to a town story.": {"ja": "町の出来事に関わる。", "ko": "마을 이야기에 얽혀 있다.", "ru": "Это связано с городской историей."},
    "It looks a little mystical.": {"ja": "少し神秘的だ。", "ko": "조금 신비롭게 보인다.", "ru": "Выглядит немного мистически."},
    "It marks a place.": {"ja": "場所を示す。", "ko": "장소를 가리킨다.", "ru": "Это отмечает место."},
    "It matches a strong theme.": {"ja": "強い題材に合う。", "ko": "강한 주제에 어울린다.", "ru": "Подходит к сильной теме."},
    "It pans up treasure.": {"ja": "宝を探り当てる。", "ko": "체질해서 보물을 찾는다.", "ru": "Им промывают сокровища."},
    "It processes farm goods.": {"ja": "農産物を加工する。", "ko": "농산물을 가공한다.", "ru": "Перерабатывает фермерские товары."},
    "It protects in combat.": {"ja": "戦いで身を守る。", "ko": "전투에서 몸을 지킨다.", "ru": "Защищает в бою."},
    "It reaches out in battle.": {"ja": "戦いで間合いを取る。", "ko": "전투에서 사거리를 뻗는다.", "ru": "Достаёт противника в бою."},
    "It stands out on the farm.": {"ja": "農場で目立つ。", "ko": "농장에서 눈에 띈다.", "ru": "Выделяется на ферме."},
    "It suits a celebration.": {"ja": "祝いに似合う。", "ko": "축하 자리에 어울린다.", "ru": "Подходит для праздника."},
    "It waters crops.": {"ja": "作物に水をやる。", "ko": "작물에 물을 준다.", "ru": "Поливает посевы."},
    "It works the soil.": {"ja": "土を耕す。", "ko": "흙을 일군다.", "ru": "Ворошит землю."},
    "The mines can hide it.": {"ja": "鉱山に潜んでいる。", "ko": "광산에 숨어 있을 수 있다.", "ru": "Это может скрываться в шахте."},
    "This one is a town local.": {"ja": "町の住人だ。", "ko": "마을 주민이다.", "ru": "Это местный житель."},
    "Townfolk care about this one.": {"ja": "町のみんなが気にかける。", "ko": "마을 사람들이 신경 쓴다.", "ru": "Горожанам это небезразлично."},
    "You can find it outdoors.": {"ja": "外で見つかる。", "ko": "바깥에서 찾을 수 있다.", "ru": "Это можно найти снаружи."},
    "You catch it in still water.": {"ja": "静かな水で釣れる。", "ko": "잔잔한 물에서 낚는다.", "ru": "Ловится в тихой воде."},
    "You could order it at the saloon.": {"ja": "酒場で頼める。", "ko": "주점에서 시킬 수 있다.", "ru": "Это можно заказать в салуне."},
    "You fight with this blade.": {"ja": "これで戦う。", "ko": "이 칼로 싸운다.", "ru": "Этим клинком сражаются."},
    "You grow it on the farm.": {"ja": "農場で育てる。", "ko": "농장에서 기른다.", "ru": "Это растят на ферме."},
    "You meet this one around town.": {"ja": "町で会える。", "ko": "마을에서 만난다.", "ru": "Его встретишь в городе."},
    "You pick it outdoors.": {"ja": "外で摘む。", "ko": "바깥에서 딴다.", "ru": "Это собирают снаружи."},
    "You plant this first.": {"ja": "まずこれを植える。", "ko": "먼저 이것부터 심는다.", "ru": "Сначала сажают именно это."},
    "You wear it on a finger.": {"ja": "指にはめる。", "ko": "손가락에 낀다.", "ru": "Это носят на пальце."},
}


RE_ABOVE = re.compile(r"^It is above (\d+)\.$")
RE_AT_MOST = re.compile(r"^It is at most (\d+)\.$")
RE_BELOW = re.compile(r"^It is below (\d+)\.$")
RE_DIGITS = re.compile(r"^Digits sum to (\d+)\.$")
RE_STARTS = re.compile(r"^It starts with (.+)\.$")
RE_FIRST_STARTS = re.compile(r"^The first word starts with (.+)\.$")
ASCII_WORD = re.compile(r"\b[A-Za-z]{3,}\b")
TOKEN_PATTERN = re.compile(r"\{\{[^}]+\}\}")


def translate_clue(locale: str, clue: str) -> str:
    if clue in SEMANTIC_CLUES:
        return SEMANTIC_CLUES[clue][locale]

    match = RE_ABOVE.match(clue)
    if match:
        n = match.group(1)
        return {"ja": f"{n}より大きい。", "ko": f"{n}보다 크다.", "ru": f"Оно больше {n}."}[locale]

    match = RE_AT_MOST.match(clue)
    if match:
        n = match.group(1)
        return {"ja": f"{n}以下だ。", "ko": f"{n} 이하다.", "ru": f"Оно не больше {n}."}[locale]

    match = RE_BELOW.match(clue)
    if match:
        n = match.group(1)
        return {"ja": f"{n}より小さい。", "ko": f"{n}보다 작다.", "ru": f"Оно меньше {n}."}[locale]

    match = RE_DIGITS.match(clue)
    if match:
        n = match.group(1)
        return {"ja": f"各桁の和は{n}。", "ko": f"자리수 합은 {n}이다.", "ru": f"Сумма цифр равна {n}."}[locale]

    match = RE_STARTS.match(clue)
    if match:
        prefix = match.group(1)
        return {"ja": f"{prefix}で始まる。", "ko": f"{prefix}로 시작한다.", "ru": f"Начинается на {prefix}."}[locale]

    match = RE_FIRST_STARTS.match(clue)
    if match:
        prefix = match.group(1)
        return {"ja": f"最初の語は{prefix}で始まる。", "ko": f"첫 단어는 {prefix}로 시작한다.", "ru": f"Первое слово начинается на {prefix}."}[locale]

    if clue == "It is even.":
        return {"ja": "偶数だ。", "ko": "짝수다.", "ru": "Оно чётное."}[locale]

    if clue == "It is odd.":
        return {"ja": "奇数だ。", "ko": "홀수다.", "ru": "Оно нечётное."}[locale]

    raise KeyError(f"Unhandled clue template: {clue}")


def rewrite_quest_overlays() -> None:
    for locale, payload in QUEST_LOCALES.items():
        path = ASSETS / f"quest-text.{locale}.json"
        path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def rewrite_magician_overlays() -> None:
    base_catalog = json.loads((ASSETS / "town-square-magician-rounds.json").read_text(encoding="utf-8"))
    base_rounds = {entry["Id"]: entry for entry in base_catalog["Rounds"]}
    for locale in ("ja", "ko", "ru"):
        path = ASSETS / f"town-square-magician-rounds.{locale}.json"
        data = json.loads(path.read_text(encoding="utf-8"))
        for round_id, entry in data["Rounds"].items():
            base_entry = base_rounds[round_id]
            entry["Prompt"] = PROMPT_MAP[locale][base_entry["Prompt"]]
            entry["OpeningLine"] = OPENING_MAP[locale][base_entry["OpeningLine"]]
            entry["VictoryLine"] = VICTORY_MAP[locale][base_entry["VictoryLine"]]
            entry["Clues"] = [translate_clue(locale, clue) for clue in base_entry.get("Clues", [])]

        path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def assert_no_question_replacement(path: Path) -> None:
    text = path.read_text(encoding="utf-8")
    if "???" in text:
        raise RuntimeError(f"{path.name} still contains question-mark replacement text")


def assert_no_english_wrappers(path: Path, is_magician: bool) -> None:
    data = json.loads(path.read_text(encoding="utf-8"))
    if is_magician:
        for round_id, entry in data["Rounds"].items():
            for field in ("Prompt", "OpeningLine", "VictoryLine"):
                if ASCII_WORD.search(entry[field]):
                    raise RuntimeError(f"{path.name} {round_id} {field} still looks English: {entry[field]!r}")
            for idx, clue in enumerate(entry["Clues"]):
                if ASCII_WORD.search(clue):
                    raise RuntimeError(f"{path.name} {round_id} Clues[{idx}] still looks English: {clue!r}")
    else:
        for group, variants in data["TitleVariants"].items():
            for idx, text in enumerate(variants):
                stripped = TOKEN_PATTERN.sub(" ", text)
                if ASCII_WORD.search(stripped):
                    raise RuntimeError(f"{path.name} TitleVariants.{group}[{idx}] still looks English: {text!r}")
        for group, text in data["SummaryTemplates"].items():
            stripped = TOKEN_PATTERN.sub(" ", text)
            if ASCII_WORD.search(stripped):
                raise RuntimeError(f"{path.name} SummaryTemplates.{group} still looks English: {text!r}")
        for group, text in data["Messages"].items():
            stripped = TOKEN_PATTERN.sub(" ", text).replace("g", " ")
            if ASCII_WORD.search(stripped):
                raise RuntimeError(f"{path.name} Messages.{group} still looks English: {text!r}")


def main() -> None:
    rewrite_quest_overlays()
    rewrite_magician_overlays()

    quest_files = [ASSETS / f"quest-text.{locale}.json" for locale in ("ja", "ko", "ru", "zh-cn")]
    magician_files = [ASSETS / f"town-square-magician-rounds.{locale}.json" for locale in ("ja", "ko", "ru")]

    for path in quest_files + magician_files:
        assert_no_question_replacement(path)

    for path in quest_files:
        assert_no_english_wrappers(path, is_magician=False)

    for path in magician_files:
        assert_no_english_wrappers(path, is_magician=True)

    print("overlay regeneration complete")


if __name__ == "__main__":
    main()
