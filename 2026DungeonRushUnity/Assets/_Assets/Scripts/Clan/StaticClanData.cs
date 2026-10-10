// Hằng số Clan / Clan War — reverse client gốc v41 (ik, ClanController.vhk, ClanWarController, CreateClanPopup…).
// Chi tiết + bằng chứng: DecodedData/CLAN_MODEL.md. Số chỉ server biết (điểm/thưởng) nằm ở server/functions/src/clanWarConfig.ts.
public static class StaticClanData
{
    public const int UNLOCK_PLAYER_LEVEL = 15;          // ClanController.vhk
    public const int CREATE_COST_GEM = 100;             // CreateClanPopup.fxy (gems > 99, trừ 0x64)
    public const int MAX_MEMBERS = 50;                  // ik.vhs / AdvancedSearchPopup.frv
    public const int NAME_MIN = 3;                      // ik.vht
    public const int MAX_CAPTAINS = 3;                  // ik.vhu
    public const int NAME_MAX = 15;                     // ik.vhv
    public const int ANNOUNCEMENT_MAX = 300;            // EditAnnouncementPopup characterLimit 0x12c
    public const int DESCRIPTION_MAX = 200;             // ClanSettingsPopup characterLimit 0xc8
    public const string NAME_REGEX = "^[A-Za-z0-9]+$";

    // Clan War (ClanWarController consts gốc).
    public const float WAR_FLUSH_DELAY = 8f;            // vph: gom action rồi gửi sau 8s
    public const float WAR_FLUSH_RETRY = 30f;           // vpi: lỗi mạng → thử lại sau 30s
    public const int WAR_MAX_PENDING_ACTIONS = 200;     // vpk
    public const float BATTLE_TAB_REFRESH = 20f;        // ClanBattleTab.vkk
    public const int DUNGEON_KEY_FALLBACK_POINTS = 500;  // ClanWarDayActionsSection.gbo

    public const string SOURCE_LOOT = "lootEquipment";
    public const string SOURCE_SUMMON_COMPANION = "summonCompanion";
    public const string SOURCE_LEVEL_UP = "levelUp";
    public const string SOURCE_MINING = "mining";
    public const string SOURCE_DUNGEON_KEY = "dungeonKey";
    public const string SOURCE_SUMMON_CAPE = "summonCape";

    public static readonly string[] ODD_DAY_SOURCES = { SOURCE_LOOT, SOURCE_SUMMON_COMPANION, SOURCE_LEVEL_UP };     // vql
    public static readonly string[] EVEN_DAY_SOURCES = { SOURCE_MINING, SOURCE_DUNGEON_KEY, SOURCE_SUMMON_CAPE };    // vqm

    // ClanWarDayActionsSection.cctor (vrc/vrd/vre).
    public static readonly string[] LOOT_RARITIES = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic", "Artifact", "Ancient", "Immortal", "Divine" };
    public static readonly string[] SUMMON_RARITIES = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
    public static readonly string[] ORES = { "Stone", "Coal", "Iron", "Ruby", "Emerald", "Gold", "Diamond" };
}
