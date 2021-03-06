﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot
{
    [HarmonyPatch(typeof(Console), "InputText")]
    public static class Console_Patch
    {
        private static readonly System.Random _random = new System.Random();

        public static bool Prefix(Console __instance)
        {
            var input = __instance.m_input.text;
            var args = input.Split(' ');
            if (args.Length == 0)
            {
                return true;
            }

            var command = args[0];
            if (command.Equals("magicitem", StringComparison.InvariantCultureIgnoreCase) ||
                command.Equals("mi", StringComparison.InvariantCultureIgnoreCase))
            {
                var rarityArg = args.Length >= 2 ? args[1] : "random";
                var itemArg = args.Length >= 3 ? args[2] : "random";
                var count = args.Length >= 4 ? int.Parse(args[3]) : 1;

                __instance.AddString($"magicitem - rarity:{rarityArg}, item:{itemArg}, count:{count}");

                var items = new List<GameObject>();
                var allItemNames = ObjectDB.instance.m_items
                    .Where(x => EpicLoot.CanBeMagicItem(x.GetComponent<ItemDrop>().m_itemData))
                    .Select(x => x.name)
                    .ToList();

                if (Player.m_localPlayer == null)
                {
                    return false;
                }

                for (var i = 0; i < count; i++)
                {
                    var rarityTable = new [] { 1, 1, 1, 1 };
                    if (rarityArg != "random" && !Enum.TryParse(rarityArg, out ItemRarity rarity))
                    {
                        __instance.AddString($"> Could not parse rarity ({rarityArg}) using random instead");
                        foreach (ItemRarity checkRarity in Enum.GetValues(typeof(ItemRarity)))
                        {
                            rarityTable[(int)checkRarity] = checkRarity == rarity ? 1 : 0;
                        }
                    }

                    var item = itemArg;
                    if (item == "random")
                    {
                        var weightedRandomTable = new WeightedRandomCollection<string>(_random, allItemNames, x => 1);
                        item = weightedRandomTable.Roll();
                    }

                    if (ObjectDB.instance.GetItemPrefab(item) == null)
                    {
                        __instance.AddString($"> Could not find item: {item}");
                        break;
                    }

                    __instance.AddString($"  {i + 1} - rarity: [{string.Join(", ", rarityTable)}], item: {item}");

                    var loot = new LootTable()
                    {
                        Object = "Console",
                        Drops = new[] { new[] { 1, 1 } },
                        Loot = new[]
                        {
                            new LootDrop()
                            {
                                Item = item,
                                Rarity = rarityTable
                            }
                        }
                    };
                    items.AddRange(EpicLoot.RollLootTableAndSpawnObjects(loot, loot.Object));
                }

                foreach (var item in items)
                {
                    var randomOffset = UnityEngine.Random.insideUnitSphere;
                    item.transform.position = Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 3 + Vector3.up * 1.5f + randomOffset;
                }

                return false;
            }

            return true;
        }
    }
}
