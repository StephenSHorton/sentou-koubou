namespace PingRage;

/// <summary>Pool of impatient end-turn ping one-liners (shuffled, no immediate repeats).</summary>
internal static class PingLines
{
    private static readonly string[] Lines =
    [
        "Anytime now.",
        "Hello? Combat? Anyone?",
        "I'm aging in real time.",
        "The enemies are waiting too, you know.",
        "End turn. I believe in you. Mostly.",
        "My coffee got cold. Still waiting.",
        "Is this a strategy or a nap?",
        "Ping. Ping. PING.",
        "I've seen turtles end turn faster.",
        "Plot twist: nothing is happening.",
        "Take your time. No, wait — don't.",
        "The Spire isn't getting any shorter.",
        "Calculating… calculating… still you.",
        "If analysis paralysis were a card, you'd draft it.",
        "I'm not mad. Just… pinging.",
        "Sir, this is a Wendy's. End turn.",
        "Your move, genius. Preferably soon.",
        "I reorganized my relics while waiting.",
        "The boss is getting bored of posing.",
        "Hurry up, I'm buffering emotional damage.",
        "Did you fall into the discard pile?",
        "This is fine. Everything is fine. Ping.",
        "Respectfully: go.",
        "I've written a novel in the meantime. It's about waiting.",
        "End. Turn. Please. Pretty please. PING.",
        "Are we speedrunning hesitation?",
        "My hand is ready. My soul is not. End turn anyway.",
        "The countdown in my head hit zero twice.",
        "Click the button. The other button. You know the one.",
        "If you need a sign, this is the sign. End turn.",
    ];

    private static readonly Random Rng = new();
    private static int _lastIndex = -1;

    public static string Next()
    {
        if (Lines.Length == 0)
            return "…";

        int index;
        if (Lines.Length == 1)
        {
            index = 0;
        }
        else
        {
            // Avoid immediate repeat.
            do
            {
                index = Rng.Next(Lines.Length);
            } while (index == _lastIndex);
        }

        _lastIndex = index;
        return Lines[index];
    }

    public static int Count => Lines.Length;
}
