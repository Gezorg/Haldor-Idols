using System;
using System.Collections.Generic;

namespace HaldorIdols
{
    /// <summary>
    /// Every vanilla trophy Haldor is willing to buy, with the default price in
    /// coins. The prices follow the biome progression: a Meadows trophy pays for
    /// a fraction of a Wooden idol, a boss trophy pays for a late-game idol.
    /// </summary>
    internal static class TrophyCatalog
    {
        internal sealed class Entry
        {
            internal readonly string Prefab;
            internal readonly string DisplayName;
            internal readonly int DefaultPrice;

            internal Entry(string prefab, string displayName, int defaultPrice)
            {
                Prefab = prefab;
                DisplayName = displayName;
                DefaultPrice = defaultPrice;
            }
        }

        internal static readonly Entry[] Entries =
        {
            // Meadows
            new Entry("TrophyBoar", "Boar Trophy", 12),
            new Entry("TrophyDeer", "Deer Trophy", 15),
            new Entry("TrophyNeck", "Neck Trophy", 10),
            new Entry("TrophyEikthyr", "Eikthyr Trophy", 250),

            // Black Forest
            new Entry("TrophyGreydwarf", "Greydwarf Trophy", 15),
            new Entry("TrophyGreydwarfBrute", "Greydwarf Brute Trophy", 40),
            new Entry("TrophyGreydwarfShaman", "Greydwarf Shaman Trophy", 35),
            new Entry("TrophySkeleton", "Skeleton Trophy", 15),
            new Entry("TrophySkeletonPoison", "Rancid Remains Trophy", 25),
            new Entry("TrophyGhost", "Ghost Trophy", 40),
            new Entry("TrophyForestTroll", "Troll Trophy", 70),
            new Entry("TrophyFrostTroll", "Troll Trophy", 70),
            new Entry("TrophyTheElder", "The Elder Trophy", 300),

            // Swamp
            new Entry("TrophyBlob", "Blob Trophy", 20),
            new Entry("TrophyDraugr", "Draugr Trophy", 20),
            new Entry("TrophyDraugrFem", "Draugr Trophy", 20),
            new Entry("TrophyDraugrElite", "Draugr Elite Trophy", 45),
            new Entry("TrophyLeech", "Leech Trophy", 15),
            new Entry("TrophySurtling", "Surtling Trophy", 25),
            new Entry("TrophyWraith", "Wraith Trophy", 40),
            new Entry("TrophyAbomination", "Abomination Trophy", 60),
            new Entry("TrophyBonemass", "Bonemass Trophy", 350),

            // Mountain
            new Entry("TrophyWolf", "Wolf Trophy", 30),
            new Entry("TrophyFenring", "Fenring Trophy", 55),
            new Entry("TrophySGolem", "Stone Golem Trophy", 85),
            new Entry("TrophyHatchling", "Drake Trophy", 50),
            new Entry("TrophyUlv", "Ulv Trophy", 45),
            new Entry("TrophyCultist", "Cultist Trophy", 50),
            new Entry("TrophyBlob_Frost", "Frost Blob Trophy", 30),
            new Entry("TrophyCultist_Hildir", "Geirrhafa Trophy", 120),
            new Entry("TrophyDragonQueen", "Moder Trophy", 400),

            // Ocean
            new Entry("TrophySerpent", "Serpent Trophy", 90),
            new Entry("TrophyBonemawSerpent", "Bonemaw Trophy", 90),

            // Plains
            new Entry("TrophyGoblin", "Fuling Trophy", 35),
            new Entry("TrophyGoblinBrute", "Fuling Berserker Trophy", 65),
            new Entry("TrophyGoblinShaman", "Fuling Shaman Trophy", 55),
            new Entry("TrophyDeathsquito", "Deathsquito Trophy", 25),
            new Entry("TrophyLox", "Lox Trophy", 85),
            new Entry("TrophyGrowth", "Growth Trophy", 40),
            new Entry("TrophyGoblinBruteBrosBrute", "Thungr Trophy", 150),
            new Entry("TrophyGoblinBruteBrosShaman", "Zil Trophy", 150),
            new Entry("TrophyGoblinKing", "Yagluth Trophy", 450),

            // Mistlands
            new Entry("TrophySeeker", "Seeker Trophy", 40),
            new Entry("TrophySeekerBrute", "Seeker Soldier Trophy", 75),
            new Entry("TrophyTick", "Tick Trophy", 15),
            new Entry("TrophyGjall", "Gjall Trophy", 65),
            new Entry("TrophyHare", "Hare Trophy", 20),
            new Entry("TrophyDvergr", "Dvergr Trophy", 35),
            new Entry("TrophySkeletonHildir", "Brenna Trophy", 150),
            new Entry("TrophySeekerQueen", "The Queen Trophy", 500),

            // Ashlands
            new Entry("TrophyCharredMelee", "Warrior Trophy", 45),
            new Entry("TrophyCharredArcher", "Marksman Trophy", 50),
            new Entry("TrophyCharredMage", "Warlock Trophy", 60),
            new Entry("TrophyAsksvin", "Asksvin Trophy", 80),
            new Entry("TrophyVolture", "Volture Trophy", 40),
            new Entry("TrophyMorgen", "Morgen Trophy", 90),
            new Entry("TrophyFallenValkyrie", "Fallen Valkyrie Trophy", 120),
            new Entry("TrophyBlob_Lava", "Lava Blob Trophy", 45),
            new Entry("TrophyWrithan", "Writhan Trophy", 70),
            new Entry("TrophyFader", "Fader Trophy", 600),

            // Deep North
            new Entry("TrophyBarka", "Barka Trophy", 70),
            new Entry("TrophyBjorn", "Bear Trophy", 90),
            new Entry("TrophyBjornUndead", "Vile Trophy", 90),
            new Entry("TrophyMoose", "Moose Trophy", 70),
            new Entry("TrophySeal", "Seal Trophy", 40),
            new Entry("TrophyElaking", "Elaking Trophy", 60),
            new Entry("TrophyMole", "Eyeless One Trophy", 80),
            new Entry("TrophyJotunWarrior", "Krigen Trophy", 120),
            new Entry("TrophyJotunWitch", "Hexen Trophy", 120),
            new Entry("TrophyKvastur", "Kvastur Trophy", 60),
            new Entry("TrophyBlob_Morkhalla", "Pulp Trophy", 70),
            new Entry("TrophyDeerWhite", "White Deer Trophy", 60)
        };

        private static readonly Dictionary<string, Entry> ByPrefab = BuildIndex();

        private static Dictionary<string, Entry> BuildIndex()
        {
            var index = new Dictionary<string, Entry>(Entries.Length, StringComparer.Ordinal);
            foreach (Entry entry in Entries) index[entry.Prefab] = entry;
            return index;
        }

        internal static bool TryGet(string prefabName, out Entry entry)
        {
            entry = null;
            return !string.IsNullOrEmpty(prefabName) && ByPrefab.TryGetValue(prefabName, out entry);
        }
    }
}
