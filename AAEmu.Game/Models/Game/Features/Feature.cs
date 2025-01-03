namespace AAEmu.Game.Models.Game.Features;

public enum Feature
{
    // updated to version 5.0.7.0
    // Don't mess with these values, ask the core team for questions.
    // Refer to FeatureSet.GetIndexes to interpret

    // Byte 0 (fset[20])
    siege = 0,                      // fset[20] & 1 == 1 (Bit 0)
    allowFamilyChanges = 1,         // Bit 1 ?
    flag_0_2 = 2,                   // Bit 2
    houseSale = 3,                  // Allows users to sell their house (Bit 3) ?
    premium = 4,                    // fset[20] & 0x10 == 0x10 (Bit 4)
    flag_0_5 = 5,                   // Bit 5
    flag_0_6 = 6,                   // Bit 6
    flag_0_7 = 7,                   // Bit 7

    // Byte 1 (fset[21])
    levelLimit = 8,                 // fset[21] (Byte 1, no bitmask) 8 -> 15 is MaxPlayerLevel
    flag_1_1 = 9,                   // Bit 1
    flag_1_2 = 10,                  // Bit 2
    flag_1_3 = 11,                  // Bit 3
    flag_1_4 = 12,                  // Bit 4
    flag_1_5 = 13,                  // Bit 5
    flag_1_6 = 14,                  // Bit 6
    flag_1_7 = 15,                  // Bit 7

    // Byte 2 (fset[22])
    flag_2_0 = 16,                  // Bit 0
    flag_2_1 = 17,                  // Bit 1
    flag_2_2 = 18,                  // Bit 2
    flag_2_3 = 19,                  // Bit 3
    flag_2_4 = 20,                  // Bit 4
    flag_2_5 = 21,                  // Bit 5
    flag_2_6 = 22,                  // Bit 6
    flag_2_7 = 23,                  // Bit 7

    // Byte 3 (fset[23])
    flag_3_0 = 24,                  // Bit 0
    flag_3_1 = 25,                  // Bit 1
    flag_3_2 = 26,                  // Bit 2
    flag_3_3 = 27,                  // Bit 3
    flag_3_4 = 28,                  // Bit 4
    flag_3_5 = 29,                  // Bit 5
    flag_3_6 = 30,                  // Bit 6
    flag_3_7 = 31,                  // Bit 7

    // Byte 4 (fset[24])
    flag_4_0 = 32,                  // Bit 0
    flag_4_1 = 33,                  // Bit 1
    flag_4_2 = 34,                  // Bit 2
    flag_4_3 = 35,                  // Bit 3
    ranking = 36,                   // fset[24] & 0x10 == 0x10 (Bit 4)
    flag_4_5 = 37,                  // Bit 5
    ingamecashshop = 38,            // fset[24] & 0x40 == 0x40 (Bit 6)
    flag_4_7 = 39,                  // Bit 7

    // Byte 5 (fset[25])
    customsaveload = 40,            // fset[24] & 0x100 == 0x100 (Bit 8)
    flag_5_1 = 41,                  // Bit 9
    flag_5_2 = 42,                  // Bit 10
    bm_mileage = 43,                // fset[24] & 0x800 == 0x800 (Bit 11)
    aaPoint = 44,                   // fset[24] & 0x1000 == 0x1000 (Bit 12)
    itemSecure = 45,                // fset[24] & 0x2000 == 0x2000 (Bit 13)
    secondpass = 46,                // fset[24] & 0x4000 == 0x4000 (Bit 14)
    flag_5_7 = 47,                  // Bit 15

    // Byte 6 (fset[26])
    slave_customize = 48,           // fset[26] & 1 == 1 (Bit 0) (Bit 16)
    flag_6_1 = 49,                  // Bit 17
    flag_6_2 = 50,                  // Bit 18
    flag_6_3 = 51,                  // Bit 19
    beautyshopBypass = 52,          // fset[24] & 0x100000 == 0x100000 (Bit 20)
    freeLpRaise = 53,               // fset[24] & 0x200000 == 0x200000 (Bit 21)
    flag_6_6 = 54,                  // Bit 22
    backpackProfitShare = 55,       // fset[24] & 0x800000 == 0x800000 (Bit 23)

    // Byte 7 (fset[27])
    flag_7_0 = 56,                  // Bit 24
    sensitiveOpeartion = 57,        // fset[24] & 0x2000000 == 0x2000000 (Bit 25)
    taxItem = 58,                   // fset[24] & 0x4000000 == 0x4000000 (Bit 26) House tax is paid using item instead of gold
    flag_7_3 = 59,                  // Bit 27
    flag_7_4 = 60,                  // Bit 28
    flag_7_5 = 61,                  // Bit 29
    achievement = 62,               // fset[24] & 0x40000000 == 0x40000000 (Bit 30)
    flag_7_7 = 63,                  // Bit 31

    // Byte 8 (fset[28])
    mateLevelLimit = 64,            // fset[28] (Byte 8, no bitmask) 64 -> 71 is MateMaxLevel
    flag_8_1 = 65,                  // Bit 1
    flag_8_2 = 66,                  // Bit 2
    flag_8_3 = 67,                  // Bit 3
    flag_8_4 = 68,                  // Bit 4
    flag_8_5 = 69,                  // Bit 5
    flag_8_6 = 70,                  // Bit 6
    flag_8_7 = 71,                  // Bit 7

    // Byte 9 (fset[29])
    flag_9_1 = 72,                  // Bit 8
    flag_9_2 = 73,                  // Bit 9
    itemChangeMapping = 74,         // fset[28] & 0x400 == 0x400 (Bit 10)
    mailCoolTime = 75,              // fset[28] & 0x800 == 0x800 (Bit 11)
    flag_9_4 = 76,                  // Bit 12
    flag_9_5 = 77,                  // Bit 13
    flag_9_6 = 78,                  // Bit 14
    flag_9_7 = 79,                  // Bit 15

    // Byte 10 (fset[30])
    flag_10_0 = 80,                 // Bit 16  setting this flag makes the client crash when connecting
    flag_10_1 = 81,                 // Bit 17
    flag_10_2 = 82,                 // Bit 18
    flag_10_3 = 83,                 // Bit 19
    flag_10_4 = 84,                 // Bit 20
    flag_10_5 = 85,                 // Bit 21
    flag_10_6 = 86,                 // Bit 22
    flag_10_7 = 87,                 // Bit 23

    // Byte 11 (fset[31])
    flag_11_0 = 88,                 // Bit 24
    hudAuctionButton = 89,          // fset[28] & 0x2000000 == 0x2000000 (Bit 25)
    hatred = 90,                    // fset[28] & 0x4000000 == 0x4000000 (Bit 26)
    auctionPostBuff = 91,           // fset[28] & 0x8000000 == 0x8000000 (Bit 27)
    itemRepairInBag = 92,           // fset[28] & 0x10000000 == 0x10000000 (Bit 28)
    petOnlyEnchantStone = 93,       // fset[28] & 0x20000000 == 0x20000000 (Bit 29)
    questNpcTag = 94,               // fset[28] & 0x40000000 == 0x40000000 (Bit 30)
    houseTaxPrepay = 95,            // fset[28] & 0x80000000 == 0x80000000 (Bit 31)

    // Byte 12 (fset[32])
    flag_12_0 = 96,                 // Bit 0
    flag_12_1 = 97,                 // Bit 1
    hudBattleFieldButton = 98,      // fset[32] & 4 == 4 (Bit 2)
    hudMailBoxButton = 99,          // fset[32] & 8 == 8 (Bit 3)
    fastQuestChatBubble = 100,      // fset[32] & 0x10 == 0x10 (Bit 4)
    todayAssignment = 101,          // fset[32] & 0x20 == 0x20 (Bit 5)
    forbidTransferChar = 102,       // fset[32] & 0x40 == 0x40 (Bit 6)
    targetEquipmentWnd = 103,       // fset[32] & 0x80 == 0x80 (Bit 7)

    // Byte 13 (fset[33])
    flag_13_0 = 104,                // Bit 8
    flag_13_1 = 105,                // Bit 9
    indunPortal = 106,              // fset[32] & 0x400 == 0x400 (Bit 10)
    flag_13_3 = 107,                // Bit 11
    flag_13_4 = 108,                // Bit 12
    flag_13_5 = 109,                // Bit 13
    indunDailyLimit = 110,          // fset[32] & 0x4000 == 0x4000 (Bit 14)
    rebuildHouse = 111,             // fset[32] & 0x8000 == 0x8000 (Bit 15)

    // Byte 14 (fset[34])
    useTGOS = 112,                  // fset[34] & 1 == 1 (Bit 0)
    forcePopupTGOS = 113,           // fset[32] & 0x20000 == 0x20000 (Bit 17)
    newNameTag = 114,               // fset[32] & 0x40000 == 0x40000 (Bit 18)
    reportSpammer = 115,            // fset[32] & 0x80000 == 0x80000 (Bit 19)
    hero = 116,                     // fset[32] & 0x100000 == 0x100000 (Bit 20)
    marketPrice = 117,              // fset[32] & 0x200000 == 0x200000 (Bit 21)
    flag_14_6 = 118,                // Bit 22
    buyPremiuminSelChar = 119,      // fset[32] & 0x800000 == 0x800000 (Bit 23)

    // Byte 15 (fset[35])
    flag_15_0 = 120,                // Bit 0
    flag_15_1 = 121,                // Bit 1
    flag_15_2 = 122,                // Bit 2
    flag_15_3 = 123,                // Bit 3
    flag_15_4 = 124,                // Bit 4
    flag_15_5 = 125,                // Bit 5
    flag_15_6 = 126,                // Bit 6
    flag_15_7 = 127,                // Bit 7

    // Byte 16 (fset[36])
    flag_16_0 = 128,                // Bit 0
    flag_16_1 = 129,                // Bit 1
    flag_16_2 = 130,                // Bit 2
    flag_16_3 = 131,                // Bit 3
    flag_16_4 = 132,                // Bit 4
    flag_16_5 = 133,                // Bit 5
    flag_16_6 = 134,                // Bit 6
    flag_16_7 = 135,                // Bit 7

    // Byte 17 (fset[37])
    flag_17_0 = 136,                // Bit 8
    flag_17_1 = 137,                // Bit 9
    expeditionWar = 138,            // fset[36] & 0x400 == 0x400 (Bit 10)
    freeResurrectionInPlace = 139,  // fset[36] & 0x800 == 0x800 (Bit 11)
    flag_17_4 = 140,                // Bit 12
    expeditionLevel = 141,          // fset[36] & 0x2000 == 0x2000 (Bit 13)
    itemEvolving = 142,             // fset[36] & 0x4000 == 0x4000 (Bit 14)
    useSavedAbilities = 143,        // fset[36] & 0x8000 == 0x8000 (Bit 15)

    // Byte 18 (fset[38])
    rankingRenewal = 144,           // fset[38] & 1 == 1 (Bit 0)
    hudAuctionBuff = 145,           // fset[36] & 0x20000 == 0x20000 (Bit 17)
    accountAttendance = 146,        // fset[36] & 0x40000 == 0x40000 (Bit 18)
    expeditionRecruit = 147,        // fset[36] & 0x80000 == 0x80000 (Bit 19)
    uiAvi = 148,                    // fset[36] & 0x100000 == 0x100000 (Bit 20)
    shopOnUI = 149,                 // fset[36] & 0x200000 == 0x200000 (Bit 21)
    itemLookConvertInBag = 150,     // fset[36] & 0x400000 == 0x400000 (Bit 22)
    newReportBaduser = 151,         // fset[36] & 0x800000 == 0x800000 (Bit 23)

    // Byte 19 (fset[39])
    flag_19_0 = 152,                // Bit 24
    expeditionSummon = 153,         // fset[36] & 0x2000000 == 0x2000000 (Bit 25)
    heroBonus = 154,                // fset[36] & 0x4000000 == 0x4000000 (Bit 26)
    flag_19_3 = 155,                // Bit 27
    flag_19_4 = 156,                // Bit 28
    flag_19_5 = 157,                // Bit 29
    hairTwoTone = 158,              // fset[36] & 0x40000000 == 0x40000000 (Bit 30)
    hudBattleFieldBuff = 159,       // fset[36] & 0x80000000 == 0x80000000 (Bit 31)

    // Byte 20 (fset[40])
    expeditionRank = 160,           // fset[40] & 1 == 1 (Bit 0)
    mateTypeSummon = 161,           // fset[40] & 2 == 2 (Bit 1)
    permissionZone = 162,           // fset[40] & 4 == 4 (Bit 2)
    lootGacha = 163,                // fset[40] & 8 == 8 (Bit 3)
    itemEvolvingReRoll = 164,       // fset[40] & 0x10 == 0x10 (Bit 4)
    flag_20_5 = 165,                // Bit 5
    ranking_myworld_only = 166,     // fset[40] & 0x40 == 0x40 (Bit 6)
    eloRating = 167,                // fset[40] & 0x80 == 0x80 (Bit 7)

    // Byte 21 (fset[41])
    nationMemberLimit = 168,        // fset[40] & 0x100 == 0x100 (Bit 8)
    flag_21_1 = 169,                // Bit 9
    packageDemolish = 170,          // fset[40] & 0x400 == 0x400 (Bit 10)
    flag_21_3 = 171,                // Bit 11
    itemGuide = 172,                // fset[40] & 0x1000 == 0x1000 (Bit 12)
    restrictFollow = 173,           // fset[40] & 0x2000 == 0x2000 (Bit 13)
    socketExtract = 174,            // fset[40] & 0x4000 == 0x4000 (Bit 14)
    itemlookExtract = 175,          // fset[40] & 0x8000 == 0x8000 (Bit 15)

    // Byte 22 (fset[42])
    useCharacterListPage = 176,     // fset[42] & 1 == 1 (Bit 0)
    renameExpeditionByItem = 177,   // fset[40] & 0x20000 == 0x20000 (Bit 17)
    flagexpeditionImmigration_22_2 = 178, // fset[40] & 0x40000 == 0x40000 (Bit 18)
    flag_22_3 = 179,                // Bit 19
    eventWebLink = 180,             // fset[40] & 0x100000 == 0x100000 (Bit 20)
    blessuthstin = 181,             // fset[40] & 0x200000 == 0x200000 (Bit 21)
    vehicleZoneSimulation = 182,    // fset[40] & 0x400000 == 0x400000 (Bit 22)
    itemSmelting = 183,             // fset[40] & 0x800000 == 0x800000 (Bit 23)

    // Byte 23 (fset[43])
    flag_23_0 = 184,                // Bit 0
    flag_23_1 = 185,                // Bit 1
    flag_23_2 = 186,                // Bit 2
    flag_23_3 = 187,                // Bit 3
    flag_23_4 = 188,                // Bit 4
    flag_23_5 = 189,                // Bit 5
    flag_23_6 = 190,                // Bit 6
    flag_23_7 = 191,                // Bit 7

    // Byte 24 (fset[44])
    characterInfoLivingPoint = 192, // fset[44] & 1 == 1 (Bit 0)
    useForceAttack = 193,           // fset[44] & 2 == 2 (Bit 1)
    specialtyTradeGoods = 194,      // fset[44] & 4 == 4 (Bit 2)
    reportBadWordUser = 195,        // fset[44] & 8 == 8 (Bit 3)
    flag_24_4 = 196,                // Bit 4
    residentweblink = 197,          // fset[44] & 0x20 == 0x20 (Bit 5)
    flag_24_6 = 198,                // Bit 6
    flag_24_7 = 199,                // Bit 7

    // Byte 25 (fset[45])
    eventCenterContentSchedule = 200, // fset[44] & 0x100 == 0x100 (Bit 8)
    auctionPartialBuy = 201,        // fset[44] & 0x200 == 0x200 (Bit 9)
    equipSlotEnchantment = 202,     // fset[44] & 0x400 == 0x400 (Bit 10)
    loadingTipOfDay = 203,          // fset[44] & 0x800 == 0x800 (Bit 11)
    flag_25_4 = 204,                // Bit 12
    itemGradeEnchant = 205,         // fset[44] & 0x2000 == 0x2000 (Bit 13)
    flag_25_6 = 206,                // Bit 14
    useHeirSkill = 207,             // fset[44] & 0x8000 == 0x8000 (Bit 15)

    // Byte 26 (fset[46])
    flag_26_0 = 208,                // Bit 16
    squad = 209,                    // fset[44] & 0x20000 == 0x20000 (Bit 17)
    eventCenterTodayAssignment = 210, // fset[44] & 0x40000 == 0x40000 (Bit 18)
    chatRace = 211,                 // fset[44] & 0x80000 == 0x80000 (Bit 19)
    flag_26_4 = 212,                // Bit 20
    flag_26_5 = 213,                // Bit 21
    flag_26_6 = 214,                // Bit 22
    flag_26_7 = 215,                // Bit 23

    // Byte 27 (fset[47])
    housingUcc = 216,               // fset[47] & 1 == 1 (Bit 0)
    protectPvp = 217,               // fset[44] & 0x2000000 == 0x2000000 (Bit 25)
    flag_27_2 = 218,                // Bit 26
    flag_27_3 = 219,                // Bit 27
    flag_27_4 = 220,                // Bit 28
    flag_27_5 = 221,                // Bit 29
    flag_27_6 = 222,                // Bit 30
    flag_27_7 = 223                 // Bit 31
}
