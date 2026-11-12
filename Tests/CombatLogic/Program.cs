using System;

internal static class Program
{
    private static int passed;
    private static int failed;

    private static int Main()
    {
        Run("Animation events without an accepted attack cause no hit", () =>
        {
            var attack = new AttackSequence();
            Reject(attack.TryConsume(CombatAttackType.Normal, 1, 0f));
            Reject(attack.TryConsume(CombatAttackType.SAttack, 1, 0f));
        });

        Run("All three special hits survive the real animation timeline", () =>
        {
            var attack = new AttackSequence();
            Accept(attack.TryBegin(CombatAttackType.SAttack, 10f, 1.5f, 3.533f));
            Accept(attack.TryConsume(CombatAttackType.SAttack, 1, 10.623f));
            Accept(attack.TryConsume(CombatAttackType.SAttack, 2, 11.164f));
            Accept(attack.TryConsume(CombatAttackType.SAttack, 3, 12.443f));
        });

        Run("Each special stage can damage only once per attack", () =>
        {
            var attack = new AttackSequence();
            Accept(attack.TryBegin(CombatAttackType.SAttack, 0f, 0.5f, 4f));
            for (int stage = 1; stage <= 3; stage++)
            {
                Accept(attack.TryConsume(CombatAttackType.SAttack, stage, stage));
                Reject(attack.TryConsume(CombatAttackType.SAttack, stage, stage));
            }
        });

        Run("Ordinary animation cannot consume a special attack hit", () =>
        {
            var attack = new AttackSequence();
            Accept(attack.TryBegin(CombatAttackType.SAttack, 0f, 0.5f, 4f));
            Reject(attack.TryConsume(CombatAttackType.Normal, 1, 1f));
            Accept(attack.TryConsume(CombatAttackType.SAttack, 1, 1f));
        });

        Run("Special animation cannot consume an ordinary attack hit", () =>
        {
            var attack = new AttackSequence();
            Accept(attack.TryBegin(CombatAttackType.Normal, 0f, 0.5f, 1f));
            Reject(attack.TryConsume(CombatAttackType.SAttack, 1, 0.7f));
            Accept(attack.TryConsume(CombatAttackType.Normal, 1, 0.7f));
        });

        Run("An ordinary attack has exactly one available stage", () =>
        {
            var attack = new AttackSequence();
            Accept(attack.TryBegin(CombatAttackType.Normal, 0f, 0.5f, 1f));
            Reject(attack.TryConsume(CombatAttackType.Normal, 2, 0.2f));
            Reject(attack.TryConsume(CombatAttackType.Normal, 3, 0.2f));
            Accept(attack.TryConsume(CombatAttackType.Normal, 1, 0.7f));
            Reject(attack.TryConsume(CombatAttackType.Normal, 1, 0.8f));
        });

        Run("Special stage indices outside one through three are rejected", () =>
        {
            var attack = new AttackSequence();
            Accept(attack.TryBegin(CombatAttackType.SAttack, 0f, 0.5f, 4f));
            Reject(attack.TryConsume(CombatAttackType.SAttack, -1, 0.3f));
            Reject(attack.TryConsume(CombatAttackType.SAttack, 0, 0.3f));
            Reject(attack.TryConsume(CombatAttackType.SAttack, 4, 0.3f));
            Accept(attack.TryConsume(CombatAttackType.SAttack, 1, 0.7f));
        });

        Run("Expired attacks cannot damage", () =>
        {
            var attack = new AttackSequence();
            Accept(attack.TryBegin(CombatAttackType.SAttack, 0f, 0.5f, 3.533f));
            Reject(attack.TryConsume(CombatAttackType.SAttack, 3, 4.1f));
        });

        Run("Animation event grace accepts delayed final hits only within its window", () =>
        {
            var attack = new AttackSequence();
            Accept(attack.TryBegin(CombatAttackType.SAttack, 0f, 0.5f, 3f, 0.25f));
            Accept(attack.TryConsume(CombatAttackType.SAttack, 1, 3.2f));
            Reject(attack.TryConsume(CombatAttackType.SAttack, 2, 3.3f));
        });

        Run("Repeated special input cannot restart an unfinished attack", () =>
        {
            var attack = new AttackSequence();
            Accept(attack.TryBegin(CombatAttackType.SAttack, 0f, 0.5f, 3.533f));
            Reject(attack.TryBegin(CombatAttackType.SAttack, 0.6f, 0.5f, 3.533f));
            Accept(attack.TryConsume(CombatAttackType.SAttack, 3, 2.443f));
        });

        Run("Ordinary input cannot replace an unfinished special attack", () =>
        {
            var attack = new AttackSequence();
            Accept(attack.TryBegin(CombatAttackType.SAttack, 0f, 0.5f, 3.533f));
            Reject(attack.TryBegin(CombatAttackType.Normal, 0.6f, 0.5f, 1f));
            Accept(attack.TryConsume(CombatAttackType.SAttack, 1, 0.7f));
        });

        Run("Special input cannot replace an unfinished ordinary attack", () =>
        {
            var attack = new AttackSequence();
            Accept(attack.TryBegin(CombatAttackType.Normal, 0f, 0.5f, 1f));
            Reject(attack.TryBegin(CombatAttackType.SAttack, 0.6f, 0.5f, 3.533f));
            Accept(attack.TryConsume(CombatAttackType.Normal, 1, 0.7f));
        });

        Run("A longer cooldown still applies after the animation finishes", () =>
        {
            var attack = new AttackSequence();
            Accept(attack.TryBegin(CombatAttackType.SAttack, 2f, 5f, 3f));
            Equal(7f, attack.ReadyAt);
            Reject(attack.TryBegin(CombatAttackType.Normal, 6.9f, 0.5f, 1f));
            Accept(attack.TryBegin(CombatAttackType.Normal, 7f, 0.5f, 1f));
        });

        Run("The animation duration extends a shorter cooldown", () =>
        {
            var attack = new AttackSequence();
            Accept(attack.TryBegin(CombatAttackType.SAttack, 2f, 0.5f, 3f));
            Equal(5f, attack.ReadyAt);
            Reject(attack.TryBegin(CombatAttackType.Normal, 4.9f, 0.5f, 1f));
            Accept(attack.TryBegin(CombatAttackType.Normal, 5f, 0.5f, 1f));
        });

        Run("Beginning the next attack clears previous stage consumption", () =>
        {
            var attack = new AttackSequence();
            Accept(attack.TryBegin(CombatAttackType.SAttack, 0f, 0.5f, 3f));
            Accept(attack.TryConsume(CombatAttackType.SAttack, 1, 0.6f));
            Accept(attack.TryConsume(CombatAttackType.SAttack, 2, 1.2f));
            Accept(attack.TryConsume(CombatAttackType.SAttack, 3, 2.4f));
            Accept(attack.TryBegin(CombatAttackType.SAttack, 3f, 0.5f, 3f));
            Accept(attack.TryConsume(CombatAttackType.SAttack, 1, 3.6f));
            Accept(attack.TryConsume(CombatAttackType.SAttack, 2, 4.2f));
            Accept(attack.TryConsume(CombatAttackType.SAttack, 3, 5.4f));
        });

        Run("Respawn reset removes pending damage and the old cooldown", () =>
        {
            var attack = new AttackSequence();
            Accept(attack.TryBegin(CombatAttackType.SAttack, 10f, 5f, 3f));
            attack.Reset();
            Reject(attack.TryConsume(CombatAttackType.SAttack, 1, 10.2f));
            Accept(attack.TryBegin(CombatAttackType.Normal, 10.2f, 0.5f, 1f));
        });

        Run("The None kind never begins or consumes an attack", () =>
        {
            var attack = new AttackSequence();
            Reject(attack.TryBegin(CombatAttackType.None, 0f, 5f, 4f));
            Reject(attack.TryConsume(CombatAttackType.None, 1, 0f));
            Accept(attack.TryBegin(CombatAttackType.Normal, 0f, 0.5f, 1f));
        });

        Console.WriteLine($"\n{passed} passed, {failed} failed.");
        return failed == 0 ? 0 : 1;
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            passed++;
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception error)
        {
            failed++;
            Console.Error.WriteLine($"FAIL {name}: {error.Message}");
        }
    }

    private static void Accept(bool actual)
    {
        if (!actual)
            throw new InvalidOperationException("Expected acceptance, but the operation was rejected.");
    }

    private static void Reject(bool actual)
    {
        if (actual)
            throw new InvalidOperationException("Expected rejection, but the operation was accepted.");
    }

    private static void Equal(float expected, float actual)
    {
        if (Math.Abs(expected - actual) > 0.0001f)
            throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}
