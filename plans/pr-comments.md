## PR COMMENTS

### Comment 1
mod/StardewLivingRPG/UI/RumorBoardMenu.cs
            foreach (var q in _state.Quests.Available.Take(5))
            {
     -           SpriteText.drawString(b, $"- {q.QuestId}: {q.Summary}", x, y, 999, width: width - 72);
     +           b.DrawString(Game1.smallFont, $"- {q.QuestId}: {q.Summary}", new Vector2(x, y), Game1.textColor);
                y += 34;
Copilot AI
12 hours ago
Switching from SpriteText.drawString (with a width constraint) to SpriteBatch.DrawString removes word-wrapping/clipping. Quest summaries (e.g., long "Emergency Town Hall..." summary) can exceed the menu width and render outside the dialogue box; consider reintroducing wrapping to (width - 72) before drawing (e.g., via a text-wrap helper) or split long lines.

### Comment 2
mod/StardewLivingRPG/UI/NewspaperMenu.cs
    +    b.DrawString(Game1.dialogueFont, $"Day {_issue.Day}: {_issue.Headline}", new Vector2(x, y), Game1.textColor);
        y += 52;

        foreach (var s in _issue.Sections)
        {
    -        SpriteText.drawString(b, s, x, y, 999, width: width - 72);
    +        b.DrawString(Game1.smallFont, s, new Vector2(x, y), Game1.textColor);
            y += 44;
Copilot AI
12 hours ago
SpriteBatch.DrawString doesn’t wrap text, so long headlines/sections (e.g., "Supply outlook: ... may stay strong if oversupply persists elsewhere.") can overflow the 840px menu width. Previously SpriteText.drawString constrained to (width - 72); add equivalent wrapping/splitting so dynamically generated section text stays within the dialogue box.

### Comment 3
mod/StardewLivingRPG/UI/MarketBoardMenu.cs

        foreach (var line in _boardService.BuildTopRows(_state, 8))
        {
    -        SpriteText.drawString(b, line, x, y, 999, width: width - 72);
    +        b.DrawString(Game1.smallFont, line, new Vector2(x, y), Game1.textColor);
            y += 40;
Copilot AI
12 hours ago
Market board rows produced by BuildTopRows include multiple numeric fields and can easily exceed the menu width; after switching to SpriteBatch.DrawString (no width constraint), lines will render past the dialogue box edge. Consider adding wrapping/truncation to fit (width - 72), or render columns with measured positions instead of a single long string.

### Comment 4
mod/StardewLivingRPG/StardewLivingRPG.csproj

+  <Target Name="CopyModToStardew" AfterTargets="Build">
+    <PropertyGroup>
+      <ModsFolder>$(SMAPI_PATH)\Mods\StardewLivingRPG</ModsFolder>
+    </PropertyGroup>
+
+    <MakeDir Directories="$(ModsFolder)" Condition="!Exists('$(ModsFolder)')" />
+
+    <Copy SourceFiles="$(OutputPath)StardewLivingRPG.dll" DestinationFolder="$(ModsFolder)" />
+    <Copy SourceFiles="$(ProjectDir)manifest.json" DestinationFolder="$(ModsFolder)" /
Copilot AI
12 hours ago
CopyModToStardew hardcodes Windows path separators ("") and runs unconditionally, which breaks cross-platform builds and can accidentally copy to an unintended location if SMAPI_PATH is empty/mis-set. Consider using Path-combine style MSBuild properties/functions for (ModsFolder)andaddaConditiontoskipthetargetwhen(SMAPI_PATH) isn’t set/doesn’t contain a Mods folder; also prefer (TargetPath)over(OutputPath)StardewLivingRPG.dll to avoid mismatches when output paths/names change.