using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;

namespace Peon.Managers
{
    [Flags]
    public enum RetainerJob : byte
    {
        Miner    = 0x01,
        Botanist = 0x02,
        Fisher   = 0x04,
        Hunter   = 0x08,
    }

    public class RetainerIdentifier
    {
        public static readonly RetainerJob[] AllJobs =
        {
            RetainerJob.Miner,
            RetainerJob.Botanist,
            RetainerJob.Fisher,
            RetainerJob.Hunter,
        };

        public static readonly RetainerJob[] Gatherers =
        {
            RetainerJob.Miner,
            RetainerJob.Botanist,
        };

        public static readonly RetainerJob[] Miner =
        {
            RetainerJob.Miner,
        };

        public static readonly RetainerJob[] Botanist =
        {
            RetainerJob.Botanist,
        };

        public static readonly RetainerJob[] Fisher =
        {
            RetainerJob.Fisher,
        };

        public static readonly RetainerJob[] Hunter =
        {
            RetainerJob.Hunter,
        };

        // @formatter:off
        private static readonly Dictionary<uint, byte> TaskSorting = new()
        {
            // Hunter
            [  12] =  1, [  10] =  2, [  11] =  3,                                                                  // 10
            [  13] =  1, [  14] =  2,                                                                               // 11
            [  15] =  1, [  16] =  2,                                                                               // 12
            [  18] =  1, [  19] =  2,                                                                               // 14
            [  20] =  1, [  21] =  2, [  22] =  3,                                                                  // 15
            [  36] =  1, [  37] =  2, [  32] =  3, [  34] =  4, [  35] =  5, [  33] =  6,                           // 25
            [  38] =  1, [  39] =  2,                                                                               // 25
            [  41] =  1, [  42] =  2,                                                                               // 28
            [  43] =  1, [  44] =  2,                                                                               // 29
            [  45] =  1, [  46] =  2, [  47] =  3,                                                                  // 30
            [  49] =  1, [  50] =  2,                                                                               // 32
            [  67] =  1, [  68] =  2,                                                                               // 49
            [ 417] =  1, [ 416] =  2, [ 418] =  3, [ 420] =  4, [ 419] =  5,                                        // 51
            [ 421] =  1, [ 422] =  2, [ 424] =  3, [ 423] =  4,                                                     // 52
            [ 425] =  1, [ 426] =  2,                                                                               // 53
            [ 427] =  1, [ 428] =  2,                                                                               // 55
            [ 429] =  1, [ 431] =  2,                                                                               // 56
            [ 432] =  1, [ 433] =  2, [ 434] =  3,                                                                  // 57
            [ 435] =  1, [ 436] =  2, [ 437] =  3, [ 438] =  4, [ 439] =  5,                                        // 58
            [ 440] =  1, [ 441] =  2, [ 442] =  3,                                                                  // 59
            [ 709] =  1, [ 710] =  2,                                                                               // 61
            [ 713] =  1, [ 712] =  2, [ 711] =  3,                                                                  // 62
            [ 714] =  1, [ 715] =  2,                                                                               // 63
            [ 719] =  1, [ 718] =  2, [ 720] =  3,                                                                  // 66
            [ 722] =  1, [ 721] =  2,                                                                               // 67
            [ 723] =  1, [ 724] =  2,                                                                               // 68
            [ 725] =  1, [ 726] =  2,                                                                               // 69
            [ 727] =  1, [ 728] =  2, [ 771] =  3,                                                                  // 70
            [ 775] =  1, [ 772] =  2, [ 773] =  3, [ 774] =  4,                                                     // 71
            [ 778] =  1, [ 777] =  2,                                                                               // 73
            [ 781] =  1, [ 780] =  2,                                                                               // 75
            [ 784] =  1, [ 783] =  2,                                                                               // 77
            [ 786] =  1, [ 787] =  2,                                                                               // 79
            // Fisher
            [ 311] =  1, [ 310] =  2,                                                                               //  5
            [ 481] =  1, [ 483] =  2, [ 501] =  3, [ 482] =  4,                                                     // 52
            [ 484] =  1, [ 485] =  2,                                                                               // 53
            [ 505] =  1, [ 486] =  2, [ 487] =  3,                                                                  // 54
            [ 502] =  1, [ 489] =  2, [ 488] =  3, [ 503] =  4, [ 490] =  5, [ 504] =  6,                           // 55
            [ 492] =  1, [ 493] =  2,                                                                               // 57
            [ 495] =  1, [ 494] =  2,                                                                               // 58
            [ 496] =  1, [ 497] =  2,                                                                               // 59
            [ 499] =  1, [ 498] =  2,                                                                               // 60
            [ 738] =  1, [ 750] =  2, [ 755] =  3,                                                                  // 70
            [ 844] =  1, [ 846] =  2,                                                                               // 74
            [ 849] =  1, [ 848] =  2,                                                                               // 78
            [ 863] =  1, [ 872] =  2,                                                                               // 80
            // Miner
            [  70] =  1, [  71] =  2, [  72] =  3, [  73] =  4, [  74] =  5, [  75] =  6,                           //  1
            [  79] =  1, [  80] =  2,                                                                               //  7
            [  83] =  1, [  84] =  2,                                                                               // 10
            [  85] =  1, [  86] =  2,                                                                               // 11
            [  89] =  1, [  90] =  2,                                                                               // 14
            [  95] =  1, [  93] =  2, [  94] =  3,                                                                  // 17
            [  97] =  1, [  98] =  2,                                                                               // 20
            [ 103] =  1, [ 104] =  2,                                                                               // 23
            [ 106] =  1, [ 107] =  2, [ 105] =  3,                                                                  // 24
            [ 108] =  1, [ 109] =  2,                                                                               // 25
            [ 110] =  1, [ 111] =  2, [ 112] =  3, [ 113] =  4, [ 114] =  5, [ 115] =  6, [ 116] =  7,              // 26
            [ 118] =  1, [ 119] =  2, [ 120] =  3, [ 121] =  4, [ 122] =  5, [ 123] =  6, [ 124] =  7, [ 125] =  8, // 28
            [ 128] =  1, [ 500] =  2, [ 129] =  3, [ 130] =  4, [ 131] =  5,                                        // 30
            [ 133] =  1, [ 132] =  2,                                                                               // 31
            [ 135] =  1, [ 136] =  2, [ 137] =  3,                                                                  // 33
            [ 138] =  1, [ 139] =  2,                                                                               // 34
            [ 146] =  1, [ 145] =  2, [ 147] =  3, [ 148] =  4, [ 149] =  5,                                        // 40
            [ 155] =  1, [ 154] =  2,                                                                               // 45
            [ 587] =  1, [ 540] =  2, [ 509] =  3, [ 510] =  4, [ 512] =  5, [ 556] =  6, [ 511] =  7, [ 575] =  8, // 50-1
            [ 543] =  9, [ 506] = 10, [ 507] = 11, [ 508] = 12, [ 514] = 13, [ 515] = 14, [ 513] = 15, [ 529] = 16, // 50-2
            [ 530] = 17, [ 531] = 18, [ 532] = 19, [ 533] = 20, [ 577] = 21, [ 538] = 22, [ 536] = 23, [ 541] = 24, // 50-3
            [ 588] =  1, [ 444] =  2, [ 446] =  3, [ 445] =  4,                                                     // 51
            [ 589] =  1, [ 447] =  2,                                                                               // 52
            [ 590] =  1, [ 545] =  2, [ 448] =  3,                                                                  // 53
            [ 591] =  1, [ 450] =  2, [ 449] =  3,                                                                  // 54
            [ 592] =  1, [ 546] =  2, [ 452] =  3, [ 451] =  4,                                                     // 55
            [ 593] =  1, [ 453] =  2,                                                                               // 56
            [ 594] =  1, [ 454] =  2, [ 455] =  3,                                                                  // 57
            [ 595] =  1, [ 547] =  2, [ 456] =  3, [ 457] =  4,                                                     // 58
            [ 596] =  1, [ 458] =  2, [ 459] =  3,                                                                  // 59
            [ 597] =  1, [ 586] =  2, [ 598] =  3, [ 739] =  4, [ 629] =  5, [ 643] =  6, [ 644] =  7, [ 602] =  8, // 60-1
            [ 601] =  9, [ 645] = 10, [ 647] = 11, [ 600] = 12, [ 628] = 13, [ 631] = 14, [ 630] = 15, [ 599] = 16, // 60-2
            [ 646] = 17, [ 460] = 18, [ 461] = 19,                                                                  // 60-3
            [ 659] =  1, [ 660] =  2, [ 648] =  3,                                                                  // 61
            [ 661] =  1, [ 649] =  2,                                                                               // 62
            [ 650] =  1, [ 662] =  2,                                                                               // 63
            [ 653] =  1, [ 652] =  2,                                                                               // 65
            [ 654] =  1, [ 663] =  2,                                                                               // 66
            [ 655] =  1, [ 656] =  2,                                                                               // 68
            [ 665] =  1, [ 666] =  2,                                                                               // 69
            [ 657] =  1, [ 658] =  2, [ 788] =  3, [ 801] =  4, [ 800] =  5, [ 744] =  6, [ 745] =  7, [ 748] =  8, // 70-1
            [ 751] =  9, [ 752] = 10, [ 756] = 11, [ 759] = 12, [ 758] = 13, [ 760] = 14,                           // 70-2
            [ 802] =  1, [ 803] =  2,                                                                               // 71
            [ 790] =  1, [ 789] =  2,                                                                               // 72
            [ 793] =  1, [ 792] =  2,                                                                               // 74
            [ 805] =  1, [ 795] =  2, [ 806] =  3, [ 794] =  4,                                                     // 76
            [ 798] =  1, [ 797] =  2,                                                                               // 78
            [ 799] =  1, [ 855] =  2, [ 857] =  3, [ 856] =  4, [ 861] =  5, [ 864] =  6, [ 865] =  7, [ 866] =  8, // 80-1
            [ 870] =  9,                                                                                            // 80-2
            // Botanist
            [ 161] =  1, [ 162] =  2, [ 163] =  3, [ 164] =  4, [ 165] =  5, [ 166] =  6, [ 167] =  7,              //  1
            [ 168] =  1, [ 169] =  2,                                                                               //  2
            [ 173] =  1, [ 172] =  2,                                                                               //  5
            [ 175] =  1, [ 174] =  2,                                                                               //  6
            [ 176] =  1, [ 177] =  2,                                                                               //  7
            [ 180] =  1, [ 179] =  2,                                                                               //  9
            [ 182] =  1, [ 181] =  2,                                                                               // 10
            [ 186] =  1, [ 185] =  2, [ 187] =  3, [ 188] =  4, [ 189] =  5, [ 557] =  6, [ 183] =  7, [ 184] =  8, // 11-1
            [ 558] =  9, [ 559] = 10,                                                                               // 11-2
            [ 191] =  1, [ 560] =  2, [ 190] =  3, [ 193] =  4, [ 192] =  5, [ 194] =  6, [ 561] =  7, [ 562] =  8, // 12
            [ 196] =  1, [ 197] =  2, [ 199] =  3, [ 200] =  4, [ 195] =  5, [ 198] =  6, [ 563] =  7, [ 564] =  8, // 13-1
            [ 565] =  9,                                                                                            // 13-2
            [ 202] =  1, [ 566] =  2, [ 203] =  3, [ 201] =  4, [ 567] =  5,                                        // 14
            [ 204] =  1, [ 205] =  2, [ 206] =  3, [ 207] =  4,                                                     // 15
            [ 208] =  1, [ 209] =  2, [ 210] =  3, [ 211] =  4, [ 212] =  5,                                        // 16
            [ 213] =  1, [ 214] =  2, [ 215] =  3,                                                                  // 17
            [ 217] =  1, [ 218] =  2, [ 216] =  3, [ 219] =  4,                                                     // 18
            [ 220] =  1, [ 221] =  2,                                                                               // 19
            [ 633] =  1, [ 634] =  2, [ 635] =  3, [ 636] =  4, [ 637] =  5, [ 638] =  6, [ 222] =  7, [ 223] =  8, // 20-1
            [ 224] =  9, [ 225] = 10, [ 226] = 11,                                                                  // 20-2
            [ 228] =  1, [ 229] =  2, [ 230] =  3,                                                                  // 21
            [ 232] =  1, [ 231] =  2,                                                                               // 22
            [ 234] =  1, [ 233] =  2, [ 235] =  3,                                                                  // 23
            [ 238] =  1, [ 236] =  2, [ 237] =  3, [ 239] =  4, [ 240] =  5,                                        // 24
            [ 241] =  1, [ 242] =  2, [ 243] =  3,                                                                  // 25
            [ 244] =  1, [ 245] =  2, [ 246] =  3, [ 247] =  4, [ 248] =  5, [ 249] =  6, [ 250] =  7,              // 26
            [ 251] =  1, [ 252] =  2,                                                                               // 27
            [ 253] =  1, [ 255] =  2, [ 256] =  3, [ 257] =  4, [ 258] =  5, [ 259] =  6, [ 260] =  7, [ 261] =  8, // 28-1
            [ 254] =  9,                                                                                            // 28-2
            [ 263] =  1, [ 264] =  2,                                                                               // 30
            [ 267] =  1, [ 266] =  2, [ 268] =  3,                                                                  // 31
            [ 270] =  1, [ 271] =  2, [ 269] =  3, [ 272] =  4,                                                     // 32
            [ 275] =  1, [ 274] =  2, [ 273] =  3, [ 276] =  4,                                                     // 33
            [ 277] =  1, [ 279] =  2, [ 278] =  3,                                                                  // 34
            [ 281] =  1, [ 282] =  2, [ 280] =  3,                                                                  // 35
            [ 283] =  1, [ 284] =  2, [ 285] =  3,                                                                  // 36
            [ 287] =  1, [ 286] =  2,                                                                               // 37
            [ 289] =  1, [ 290] =  2, [ 291] =  3,                                                                  // 39
            [ 292] =  1, [ 293] =  2,                                                                               // 40
            [ 295] =  1, [ 296] =  2, [ 297] =  3,                                                                  // 42
            [ 298] =  1, [ 299] =  2,                                                                               // 43
            [ 305] =  1, [ 306] =  2,                                                                               // 49
            [ 604] =  1, [ 620] =  2, [ 568] =  3, [ 571] =  4, [ 570] =  5, [ 521] =  6, [ 516] =  7, [ 518] =  8, // 50-1
            [ 517] =  9, [ 519] = 10, [ 520] = 11, [ 569] = 12, [ 522] = 13, [ 523] = 14, [ 524] = 15, [ 574] = 16, // 50-2
            [ 544] = 17, [ 573] = 18, [ 527] = 19, [ 572] = 20, [ 528] = 21, [ 534] = 22, [ 535] = 23, [ 525] = 24, // 50-3
            [ 526] = 25, [ 576] = 26, [ 539] = 27, [ 537] = 28, [ 542] = 29,                                        // 50-4
            [ 605] =  1, [ 463] =  2, [ 462] =  3, [ 464] =  4,                                                     // 51
            [ 606] =  1, [ 621] =  2, [ 465] =  3,                                                                  // 52
            [ 607] =  1, [ 622] =  2, [ 548] =  3, [ 466] =  4,                                                     // 53
            [ 608] =  1, [ 468] =  2, [ 467] =  3,                                                                  // 54
            [ 609] =  1, [ 470] =  2, [ 550] =  3, [ 549] =  4, [ 469] =  5,                                        // 55
            [ 610] =  1, [ 471] =  2,                                                                               // 56
            [ 612] =  1, [ 472] =  2, [ 473] =  3,                                                                  // 57
            [ 611] =  1, [ 551] =  2, [ 474] =  3, [ 475] =  4,                                                     // 58
            [ 613] =  1, [ 478] =  2, [ 477] =  3, [ 476] =  4,                                                     // 59
            [ 603] =  1, [ 618] =  2, [ 619] =  3, [ 641] =  4, [ 680] =  5, [ 617] =  6, [ 616] =  7, [ 642] =  8, // 60-1
            [ 639] =  9, [ 615] = 10, [ 625] = 11, [ 627] = 12, [ 626] = 13, [ 667] = 14, [ 624] = 15, [ 623] = 16, // 60-2
            [ 640] = 17, [ 681] = 18, [ 614] = 19, [ 632] = 20, [ 479] = 21,                                        // 60-3
            [ 668] =  1, [ 669] =  2,                                                                               // 61
            [ 683] =  1, [ 682] =  2, [ 684] =  3,                                                                  // 62
            [ 670] =  1, [ 686] =  2, [ 685] =  3,                                                                  // 63
            [ 687] =  1, [ 688] =  2, [ 689] =  3, [ 690] =  4, [ 671] =  5,                                        // 64
            [ 691] =  1, [ 673] =  2, [ 674] =  3, [ 692] =  4, [ 672] =  5, [ 693] =  6,                           // 65
            [ 694] =  1, [ 695] =  2, [ 696] =  3, [ 697] =  4, [ 698] =  5, [ 675] =  6,                           // 66
            [ 676] =  1, [ 699] =  2, [ 677] =  3,                                                                  // 67
            [ 700] =  1, [ 701] =  2, [ 702] =  3, [ 703] =  4, [ 704] =  5, [ 705] =  6,                           // 68
            [ 706] =  1, [ 707] =  2,                                                                               // 69
            [ 679] =  1, [ 818] =  2, [ 678] =  3, [ 708] =  4, [ 746] =  5, [ 747] =  6, [ 749] =  7, [ 753] =  8, // 70-1
            [ 754] =  9, [ 757] = 10, [ 761] = 11, [ 762] = 12,                                                     // 70-2
            [ 821] =  1, [ 822] =  2, [ 819] =  3, [ 809] =  4, [ 808] =  5, [ 810] =  6, [ 820] =  7,              // 71
            [ 824] =  1, [ 823] =  2, [ 811] =  3, [ 825] =  4,                                                     // 72
            [ 827] =  1, [ 812] =  2, [ 828] =  3,                                                                  // 74
            [ 830] =  1, [ 829] =  2, [ 813] =  3,                                                                  // 75
            [ 831] =  1, [ 814] =  2, [ 815] =  3, [ 832] =  4,                                                     // 76
            [ 839] =  1, [ 816] =  2, [ 835] =  3, [ 834] =  4, [ 836] =  5,                                        // 78
            [ 840] =  1, [ 837] =  2, [ 838] =  3,                                                                  // 79
            [ 817] =  1, [ 858] =  2, [ 859] =  3, [ 860] =  4, [ 862] =  5, [ 867] =  6, [ 868] =  7, [ 869] =  8, // 80-1
            [ 871] =  9,                                                                                            // 80-2
        };
        // @formatter:on

        public struct RetainerTaskInfo
        {
            public byte Category;
            public byte LevelRange;
            public byte Item;
        }

        public readonly Dictionary<RetainerJob, Dictionary<string, RetainerTaskInfo>> Tasks = new()
        {
            [RetainerJob.Botanist] = new Dictionary<string, RetainerTaskInfo>(),
            [RetainerJob.Miner]    = new Dictionary<string, RetainerTaskInfo>(),
            [RetainerJob.Fisher]   = new Dictionary<string, RetainerTaskInfo>(),
            [RetainerJob.Hunter]   = new Dictionary<string, RetainerTaskInfo>(),
        };

        private static RetainerJob FromClassJobCategory(byte classJobCategory)
        {
            return classJobCategory switch
            {
                34  => RetainerJob.Hunter,
                32  => RetainerJob.Botanist | RetainerJob.Miner | RetainerJob.Fisher,
                18  => RetainerJob.Botanist,
                17  => RetainerJob.Miner,
                19  => RetainerJob.Fisher,
                154 => RetainerJob.Botanist | RetainerJob.Miner,
                _   => 0,
            };
        }

        private static Dictionary<uint, byte> ExplorationsToItem()
        {
            var  randomTasks      = Dalamud.GameData.GetExcelSheet<RetainerTaskRandom>(ClientLanguage.English)!;
            var  ret              = new Dictionary<uint, byte>((int) randomTasks.RowCount);
            byte watersideCounter = 0;
            byte woodlandCounter  = 0;
            byte highlandCounter  = 0;
            byte fieldCounter     = 0;

            foreach (var task in randomTasks.Reverse())
            {
                var name = task.Name.ToString();
                if (name.StartsWith("Water"))
                    ret.Add(task.RowId, watersideCounter++);
                else if (name.StartsWith("Wood"))
                    ret.Add(task.RowId, woodlandCounter++);
                else if (name.StartsWith("High"))
                    ret.Add(task.RowId, highlandCounter++);
                else if (name.StartsWith("Field"))
                    ret.Add(task.RowId, fieldCounter++);
                else if (name.StartsWith("Quick"))
                    continue;
                else
                    PluginLog.Error($"Random exploration {name} could not be corresponded to a job exploration.");
            }

            return ret;
        }

        public bool Identify(string name, RetainerJob job, out RetainerTaskInfo info)
        {
            if (!Tasks.TryGetValue(job, out var tasks))
            {
                info = default;
                PluginLog.Error("Invalid retainer job requested.");
                return false;
            }

            name = name.ToLowerInvariant();
            return tasks.TryGetValue(name, out info);
        }

        private static List<RetainerTask>[] CreateLists(int count)
        {
            var lists = new List<RetainerTask>[count];
            for (var i = 0; i < count; ++i)
                lists[i] = new List<RetainerTask>();
            return lists;
        }

        private static int Compare(RetainerTask t1, RetainerTask t2)
        {
            if (t1.RowId == t2.RowId)
                return 0;
            if (t1.RetainerLevel != t2.RetainerLevel)
                return t1.RetainerLevel.CompareTo(t2.RetainerLevel);

            if (TaskSorting.TryGetValue(t1.RowId, out var v1))
                if (TaskSorting.TryGetValue(t2.RowId, out var v2))
                    return v1.CompareTo(v2);
                else
                    return -1;
            else if (TaskSorting.TryGetValue(t2.RowId, out var _))
                return 1;
            else
                return t1.RowId.CompareTo(t2.RowId);
        }

        public RetainerIdentifier()
        {
            var tasks        = Dalamud.GameData.GetExcelSheet<RetainerTask>()!;
            var normalTasks  = Dalamud.GameData.GetExcelSheet<RetainerTaskNormal>()!;
            var randomTasks  = Dalamud.GameData.GetExcelSheet<RetainerTaskRandom>(Dalamud.ClientState.ClientLanguage)!;
            var items        = Dalamud.GameData.GetExcelSheet<Item>(Dalamud.ClientState.ClientLanguage)!;
            var levelRanges  = Dalamud.GameData.GetExcelSheet<RetainerTaskLvRange>()!;
            var explorations = ExplorationsToItem();

            var counters = new Dictionary<RetainerJob, List<RetainerTask>[]>(4)
            {
                [RetainerJob.Botanist] = CreateLists((int) levelRanges.RowCount),
                [RetainerJob.Miner]    = CreateLists((int) levelRanges.RowCount),
                [RetainerJob.Fisher]   = CreateLists((int) levelRanges.RowCount),
                [RetainerJob.Hunter]   = CreateLists((int) levelRanges.RowCount),
            };

            foreach (var task in tasks.Where(t => t.Task != 0))
            {
                var jobs = FromClassJobCategory((byte) task.ClassJobCategory.Row);
                if (jobs == 0)
                    continue;

                if (task.Task < 30000)
                {
                    foreach (var flag in counters.Where(flag => jobs.HasFlag(flag.Key)))
                        flag.Value[(byte) ((task.RetainerLevel - 1) / 5)].Add(task);
                }
                else
                {
                    var name = randomTasks.GetRow(task.Task)!.Name.ToString().ToLowerInvariant();
                    var taskInfo = new RetainerTaskInfo
                    {
                        Category   = (byte) (task.Task == 30053 ? 2 : 1),
                        LevelRange = 0,
                        Item       = (byte) (task.Task == 30053 ? 0 : explorations[task.Task]),
                    };

                    Tasks[jobs].Add(name, taskInfo);
                }
            }

            foreach (var job in counters)
                for (var j = 0; j < job.Value.Length; ++j)
                {
                    var list = job.Value[j];
                    list.Sort(Compare);
                    for (var i = 0; i < list.Count; ++i)
                    {
                        var task       = list[i];
                        var normalTask = normalTasks.GetRow(task.Task);
                        var name       = items.GetRow(normalTask!.Item.Row)!.Name.ToString().ToLowerInvariant();
                        var taskInfo = new RetainerTaskInfo()
                        {
                            Category   = 0,
                            Item       = (byte) i,
                            LevelRange = (byte) j,
                        };
                        Tasks[job.Key].Add(name, taskInfo);
                    }
                }
        }
    }
}
