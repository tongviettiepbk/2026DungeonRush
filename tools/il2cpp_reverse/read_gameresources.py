# -*- coding: utf-8 -*-
import os, io, zipfile, json, struct, glob
import UnityPy
from UnityPy.helpers.TypeTreeNode import TypeTreeNode

SP = os.path.dirname(os.path.abspath(__file__))
XAPK = r"E:\Project\2026DungeonRush\Dungeon+Rush_41_APKPure.xapk"
DATA_DIR = os.path.join(SP, "bin_data")
DUMMY = os.path.join(SP, "dump", "DummyDll")

if not os.path.isdir(DATA_DIR) or not os.listdir(DATA_DIR):
    os.makedirs(DATA_DIR, exist_ok=True)
    with zipfile.ZipFile(XAPK) as outer:
        base = outer.read("com.lavalabs.dungeonrush.apk")
        with zipfile.ZipFile(io.BytesIO(base)) as apk:
            for n in apk.namelist():
                if n.startswith("assets/bin/Data/") and not n.endswith("/"):
                    rel = n[len("assets/bin/Data/"):]
                    dst = os.path.join(DATA_DIR, rel.replace("/", os.sep))
                    os.makedirs(os.path.dirname(dst), exist_ok=True)
                    open(dst, "wb").write(apk.read(n))
    print("extracted bin/Data")

# typetree for GameResources
from TypeTreeGeneratorAPI import TypeTreeGenerator
gen = TypeTreeGenerator("2022.3.62f2")
for f in glob.glob(os.path.join(DUMMY, "*.dll")):
    try: gen.load_dll(open(f, "rb").read())
    except Exception: pass
try: gen.set_add_mono_behaviour_root_nodes(1)
except Exception: pass
gr_json = json.loads(gen.get_nodes_as_json("Assembly-CSharp.dll", "GameResources"))
gr_tree = TypeTreeNode.from_list(gr_json)
print("GameResources nodes:", len(gr_json))

env = UnityPy.load(DATA_DIR)

# map MonoScript (file,pathid) -> classname
script_name = {}
for obj in env.objects:
    if obj.type.name == "MonoScript":
        try:
            d = obj.read()
            script_name[(obj.assets_file, obj.path_id)] = getattr(d, "m_ClassName", None)
        except Exception:
            pass

def resolve_script(obj):
    data = obj.get_raw_data()
    if len(data) < 28: return None
    fid = struct.unpack_from("<i", data, 16)[0]
    pid = struct.unpack_from("<q", data, 20)[0]
    src = obj.assets_file
    if fid == 0:
        return script_name.get((src, pid))
    ext = src.externals[fid-1]
    nm = ext.path.split("/")[-1].lower()
    for af in env.files.values():
        if getattr(af, "name", "") and af.name.lower() == nm:
            return script_name.get((af, pid))
    return None

keys = ["ItemStatBaseMeleeWeapon","ItemStatBaseRangedWeapon","ItemStatBaseGlovesDamage",
        "ItemStatBaseHeadItem","ItemStatBaseBackItem","ItemStatBaseNecklaceHealth",
        "ItemStatBaseRingDamage","ItemStatTierScaler","ItemStatLevelScaler",
        "PlayerBaseWeaponDamage","PlayerBaseGlovesDamage","PlayerBaseHelmetHealth",
        "PlayerBaseBackpackHealth","PlayerBaseNecklaceHealth","PlayerBaseRingDamage",
        "PlayerBaseAttackSpeed","PlayerBaseMoveSpeed","PlayerBaseAttackDistance",
        "EnemyBaseMoveSpeed","EnemyBaseMeleeAttackDistance","EnemyBaseRangeAttackDistance",
        "EnemyDetectionRange","EnemyGearSeed","EnemyGearMaxCombatLevel","EnemyGearTierWeights",
        "ArmyPowerBase","ArmyPowerExponentialScaler","ArmyPowerSegments",
        "MeleeHealthToDamageRatio","RangedHealthToDamageRatio","BossHealthToDamageRatio",
        "RangedUnitDamageMultiplier","PresetSeed","ArmyPresets","ManualPresets",
        "DragonLevelBase","DragonLevelToCombatLevelMultiplier",
        "ZombieLevelBase","ZombieLevelToCombatLevelMultiplier","ZombiePresets",
        "CultistLevelBase","CultistLevelToCombatLevelMultiplier","CultistPresets",
        "BaseCriticalDamagePercent","BaseGameSpeed","DragonBossCharacterId",
        "ExperienceLevelBase","ExperienceLevelScaler","HealthRegenMultiplier"]

found = 0
for obj in env.objects:
    if obj.type.name != "MonoBehaviour":
        continue
    if resolve_script(obj) != "GameResources":
        continue
    found += 1
    tree = obj.read_typetree(gr_tree)
    print("===== GameResources values =====")
    result = {}
    for k in keys:
        v = tree.get(k)
        result[k] = v
        if isinstance(v, list):
            print(f"{k} = [{len(v)}] {json.dumps(v, default=str)[:400]}")
        else:
            print(f"{k} = {v}")
    json.dump(result, open(os.path.join(SP, "gameresources_values.json"), "w"), indent=2, default=str)
    break

print("found GameResources MB:", found)
