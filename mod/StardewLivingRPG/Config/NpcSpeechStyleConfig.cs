namespace StardewLivingRPG.Config;

public enum NpcVerbalProfile
{
    Professional,
    Traditionalist,
    Intellectual,
    Enthusiast,
    Recluse
}

public sealed class NpcSpeechStyleConfig
{
    public string DefaultProfile { get; set; } = nameof(NpcVerbalProfile.Traditionalist);
    public int HighCharismaThreshold { get; set; } = 7;
    public int HighSocialThreshold { get; set; } = 7;
    public Dictionary<string, string> NpcProfiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static NpcSpeechStyleConfig CreateDefault()
    {
        return new NpcSpeechStyleConfig
        {
            NpcProfiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Lewis"] = nameof(NpcVerbalProfile.Professional),
                ["Pierre"] = nameof(NpcVerbalProfile.Professional),
                ["Robin"] = nameof(NpcVerbalProfile.Professional),
                ["Clint"] = nameof(NpcVerbalProfile.Professional),
                ["Gus"] = nameof(NpcVerbalProfile.Professional),
                ["Willy"] = nameof(NpcVerbalProfile.Professional),

                ["Caroline"] = nameof(NpcVerbalProfile.Traditionalist),
                ["Evelyn"] = nameof(NpcVerbalProfile.Traditionalist),
                ["George"] = nameof(NpcVerbalProfile.Traditionalist),
                ["Jodi"] = nameof(NpcVerbalProfile.Traditionalist),
                ["Kent"] = nameof(NpcVerbalProfile.Traditionalist),
                ["Marnie"] = nameof(NpcVerbalProfile.Traditionalist),
                ["Pam"] = nameof(NpcVerbalProfile.Traditionalist),
                ["Penny"] = nameof(NpcVerbalProfile.Traditionalist),
                ["Leah"] = nameof(NpcVerbalProfile.Traditionalist),

                ["Demetrius"] = nameof(NpcVerbalProfile.Intellectual),
                ["Harvey"] = nameof(NpcVerbalProfile.Intellectual),
                ["Maru"] = nameof(NpcVerbalProfile.Intellectual),
                ["Gunther"] = nameof(NpcVerbalProfile.Intellectual),
                ["Wizard"] = nameof(NpcVerbalProfile.Intellectual),
                ["Qi"] = nameof(NpcVerbalProfile.Intellectual),
                ["Elliott"] = nameof(NpcVerbalProfile.Intellectual),

                ["Abigail"] = nameof(NpcVerbalProfile.Enthusiast),
                ["Alex"] = nameof(NpcVerbalProfile.Enthusiast),
                ["Emily"] = nameof(NpcVerbalProfile.Enthusiast),
                ["Haley"] = nameof(NpcVerbalProfile.Enthusiast),
                ["Leo"] = nameof(NpcVerbalProfile.Enthusiast),
                ["Sam"] = nameof(NpcVerbalProfile.Enthusiast),
                ["Vincent"] = nameof(NpcVerbalProfile.Enthusiast),
                ["Jas"] = nameof(NpcVerbalProfile.Enthusiast),
                ["Sandy"] = nameof(NpcVerbalProfile.Enthusiast),

                ["Linus"] = nameof(NpcVerbalProfile.Recluse),
                ["Krobus"] = nameof(NpcVerbalProfile.Recluse),
                ["Shane"] = nameof(NpcVerbalProfile.Recluse),
                ["Dwarf"] = nameof(NpcVerbalProfile.Recluse),
                ["Sebastian"] = nameof(NpcVerbalProfile.Recluse),
                ["Marlon"] = nameof(NpcVerbalProfile.Recluse),
                ["Gil"] = nameof(NpcVerbalProfile.Recluse),
                ["Morris"] = nameof(NpcVerbalProfile.Professional)
            }
        };
    }
}
