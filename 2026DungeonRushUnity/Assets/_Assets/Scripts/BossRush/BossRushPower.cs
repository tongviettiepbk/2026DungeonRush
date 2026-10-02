using System;

// Power gửi lên Boss Rush — GỐC rm.iqm → rm.iqj: Power = (Attack × (1 + Damage%)) × (HP × (1 + HP%)),
// tức tích Attack cuối × Máu tối đa cuối của hero (đã gồm đồ, Own Effect pet, substat %). Làm tròn xuống (rm.iqm → long).
public static class BossRushPower
{
    public static double GetPlayerPower()
    {
        GameController controller = GameController.Instance;
        HeroUnit hero = controller != null && controller.mode != null ? controller.mode.Hero : null;
        if (hero == null || hero.stats == null)
        {
            return 0d;
        }
        return Math.Floor(hero.stats.attack * hero.GetMaxHp());
    }
}
