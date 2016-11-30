using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Jinx
{
    class Program
    {
        internal static Menu Root;
        internal static Spell Q, W, E, R;
        internal static Orbwalking.Orbwalker Orbwalker;
        internal static float RocketRange;
        internal static float FarmRadius => Items.HasItem(3085) ? 300f: 150f;
        internal static Obj_AI_Hero Player => ObjectManager.Player;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != "Jinx")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 600);

            W = new Spell(SpellSlot.W, 1500f);
            W.SetSkillshot(0.6f, 60f, 3300f, true, SkillshotType.SkillshotLine);

            E = new Spell(SpellSlot.E, 900f);
            E.SetSkillshot(1.2f, 120f, 2300f, false, SkillshotType.SkillshotCircle);

            R = new Spell(SpellSlot.R, 3000);
            R.SetSkillshot(0.6f, 140f, 1700f, false, SkillshotType.SkillshotLine);

            Root = new Menu("ProSeries: Jinx", "jinx", true);

            var ormenu = new Menu("Orbwalk", "ormenu");
            Orbwalker = new Orbwalking.Orbwalker(ormenu);
            Root.AddSubMenu(ormenu);

            var kemenu = new Menu("Keys", "kemenu");
            kemenu.AddItem(new MenuItem("usecombo", "Combo [active]")).SetValue(new KeyBind(32, KeyBindType.Press));
            kemenu.AddItem(new MenuItem("useharass", "Harass [active]")).SetValue(new KeyBind('G', KeyBindType.Press));
            kemenu.AddItem(new MenuItem("useclear", "Wave/Jungle [active]")).SetValue(new KeyBind(86, KeyBindType.Press));
            kemenu.AddItem(new MenuItem("useflee", "Flee [active]")).SetValue(new KeyBind('A', KeyBindType.Press));
            Root.AddSubMenu(kemenu);

            var comenu = new Menu("Combo", "cmenu");

            var tcmenu = new Menu("Config", "tcmenu");


            tcmenu.AddItem(new MenuItem("autor", "Auto R Killable")).SetValue(true);
            tcmenu.AddItem(new MenuItem("autoe", "Auto E Immobile")).SetValue(true);
            tcmenu.AddItem(new MenuItem("autoedash", "Auto E Dashing")).SetValue(true);
            tcmenu.AddItem(new MenuItem("autoetele", "Auto E Teleport")).SetValue(true);
            comenu.AddSubMenu(tcmenu);

            comenu.AddItem(new MenuItem("useqcombo", "Use Q")).SetValue(true);
            comenu.AddItem(new MenuItem("usewcombo", "Use W")).SetValue(true);
            comenu.AddItem(new MenuItem("useecombo", "Use E")).SetValue(false);
            comenu.AddItem(new MenuItem("usercombo", "Use R")).SetValue(true);
            Root.AddSubMenu(comenu);

            var hamenu = new Menu("Harass", "hamenu");

            var wList = new Menu("Harass Whitelist", "hwl");
            foreach (var enemy in HeroManager.Enemies)
            {
                wList.AddItem(new MenuItem("hwl" + enemy.ChampionName, enemy.ChampionName))
                    .SetValue(true);
            }

            hamenu.AddSubMenu(wList);
            hamenu.AddItem(new MenuItem("useqharass", "Use Q")).SetValue(true);
            hamenu.AddItem(new MenuItem("useqharassminion", "-> Only On Minion")).SetValue(true);
            hamenu.AddItem(new MenuItem("qharassmana", "-> Minimum mana %")).SetValue(new Slider(65));
            hamenu.AddItem(new MenuItem("usewharass", "Use W")).SetValue(true);
            hamenu.AddItem(new MenuItem("wharassmana", "-> Minimum mana %")).SetValue(new Slider(70));
            hamenu.AddItem(new MenuItem("autoqharass", "Auto Q Minion Harass").SetValue(true));
            hamenu.AddItem(new MenuItem("autoqharassmana", "-> Minimum mana %")).SetValue(new Slider(55));

            Root.AddSubMenu(hamenu);

            var wMenu = new Menu("Farming", "farming");
            wMenu.AddItem(new MenuItem("useqclear", "Use Q").SetValue(true));
            wMenu.AddItem(new MenuItem("useqclearkill", "-> Only if Will Kill")).SetValue(true);
            wMenu.AddItem(new MenuItem("clearqmin", "Minimum minion count")).SetValue(new Slider(3, 2, 6));
            wMenu.AddItem(new MenuItem("waveclearmana", "Wave Minimum mana %")).SetValue(new Slider(75));
            wMenu.AddItem(new MenuItem("jungleclearmana", "Jungle Minimum mana %")).SetValue(new Slider(35));
            Root.AddSubMenu(wMenu);

            var fmenu = new Menu("Flee", "fmenu");
            Root.AddSubMenu(fmenu);

            var exmenu = new Menu("Extra", "exmenu");
            exmenu.AddItem(new MenuItem("harasswc", "Harass in Wave Clear")).SetValue(true);
            exmenu.AddItem(new MenuItem("minrdist", "Min R Distance")).SetValue(new Slider(450, 0, 3000));
            exmenu.AddItem(new MenuItem("maxrdist", "Max R distance")).SetValue(new Slider(1500, 0, 3000));
            //exmenu.AddItem(new MenuItem("interrupt", "Interrupter")).SetValue(false);
            //exmenu.AddItem(new MenuItem("gap", "Anti-Gapcloser")).SetValue(false);
            Root.AddSubMenu(exmenu);

            var skmenu = new Menu("Skins", "skmenu");
            var skinitem = new MenuItem("useskin", "Enabled");
            skmenu.AddItem(skinitem).SetValue(false);

            skinitem.ValueChanged += (sender, eventArgs) =>
            {
                if (!eventArgs.GetNewValue<bool>())
                {
                    ObjectManager.Player.SetSkin(ObjectManager.Player.CharData.BaseSkinName, ObjectManager.Player.BaseSkinId);
                }
            };

            skmenu.AddItem(new MenuItem("skinid", "Skin Id")).SetValue(new Slider(4, 0, 12));
            Root.AddSubMenu(skmenu);

            var drmenu = new Menu("Draw", "drmenu");
            drmenu.AddItem(new MenuItem("drawe", "Draw E")).SetValue(false).ValueChanged +=
                (sender, eventArgs) => eventArgs.Process = false;
            drmenu.AddItem(new MenuItem("draww", "Draw W")).SetValue(false).ValueChanged +=
                (sender, eventArgs) => eventArgs.Process = false;
            Root.AddSubMenu(drmenu);

            Root.AddToMainMenu();

            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Game.OnUpdate += Game_OnUpdate;

            Game.PrintChat("<b><font color=\"#FF3366\">ProSeries: Jinx</font></b> - Loaded!");
        }

        private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            var aiHero = args.Target as Obj_AI_Hero;
            var aiMinion = args.Target as Obj_AI_Minion;

            var minigunOut = Player.GetSpell(SpellSlot.Q).ToggleState == 1;

            if (aiMinion != null && !aiMinion.IsDead)
            {
                if (CanHarass || (CanClear && Root.Item("harasswc").GetValue<bool>()))
                {
                    if (Player.Mana / Player.MaxMana * 100 < Root.Item("qharassmana").GetValue<Slider>().Value)
                    {
                        return;
                    }

                    foreach (var enemy in HeroManager.Enemies.Where(x => Root.Item("hwl" + x.ChampionName).GetValue<bool>()))
                    {
                        if (enemy.Distance(aiMinion.ServerPosition) <= FarmRadius)
                        {
                            if (minigunOut)
                            {
                                Q.Cast();
                            }
                        }
                    }

                    return;
                }

                if (!Root.Item("autoqharass").GetValue<bool>())
                {
                    return;
                }

                if (Player.Mana / Player.MaxMana * 100 < Root.Item("autoqharassmana").GetValue<Slider>().Value)
                {
                    return;
                }

                foreach (var enemy in HeroManager.Enemies.Where(x => Root.Item("hwl" + x.ChampionName).GetValue<bool>()))
                {
                    if (enemy.Distance(aiMinion.ServerPosition) <= FarmRadius)
                    {
                        if (minigunOut)
                        {
                            Q.Cast();
                        }
                    }
                }
            }
        }

        internal static bool CanCombo => Root.Item("usecombo").GetValue<KeyBind>().Active;

        internal static bool CanHarass => Root.Item("useharass").GetValue<KeyBind>().Active;

        internal static bool CanClear => Root.Item("useclear").GetValue<KeyBind>().Active;

        private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var aiHero = target as Obj_AI_Hero;
            if (aiHero != null && unit.IsMe)
            {
                if (aiHero.Distance(Player.ServerPosition) > 525)
                {
                    if (Player.GetSpellDamage(aiHero, SpellSlot.W) / W.Delay >
                        Player.GetAutoAttackDamage(aiHero, true) * (1 / Player.AttackDelay))
                    {
                        W.Cast(aiHero);
                    }
                }
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            RocketRange = new[] { 75, 75, 100, 125, 150, 175 }[Q.Level];
            var minigunOut = Player.GetSpell(SpellSlot.Q).ToggleState == 1;

            if (Root.Item("useskin").GetValue<bool>())
            {
                Player.SetSkin(Player.CharData.BaseSkinName, Root.Item("skinid").GetValue<Slider>().Value);
            }

            if (CanCombo)
            {
                if (!minigunOut && Root.Item("useqcombo").GetValue<bool>())
                {
                    if (Player.ManaPercent < 35 &&
                        HeroManager.Enemies.Any(
                            i => i.IsValidTarget(590 + RocketRange + 10) &&
                                    Player.GetAutoAttackDamage(i, true) * 3 < i.Health))
                    {
                        Q.Cast();
                    }
                }

                var qtarget = TargetSelector.GetTarget(525 + RocketRange + 250, TargetSelector.DamageType.Physical);
                if (qtarget.IsValidTarget() && Q.IsReady())
                {
                    if (Root.Item("useqcombo").GetValue<bool>())
                    {
                        if (minigunOut && (Player.ManaPercent > 35 ||
                            Player.GetAutoAttackDamage(qtarget, true) * 3 > qtarget.Health) &&
                            qtarget.Distance(Player.ServerPosition) > 525)
                            Q.Cast();

                        if (!minigunOut && Player.ManaPercent < 35)
                        {
                            Q.Cast();
                        }

                        if (!minigunOut && qtarget.Distance(Player.ServerPosition) <= 525)
                        {
                            Q.Cast();
                        }
                    }
                }

                var wtarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (wtarget.IsValidTarget() && W.IsReady() && !Player.IsWindingUp)
                {
                    if (Root.Item("usewcombo").GetValue<bool>())
                    {
                        if (wtarget.Distance(Player.ServerPosition) > 500 + RocketRange)
                        {
                            if (Player.GetAutoAttackDamage(wtarget, true) * 2 < wtarget.Health)
                                W.CastIfHitchanceEquals(wtarget, HitChance.High);
                        }
                    }
                }

                if (Root.Item("useecombo").GetValue<bool>() && E.IsReady())
                {
                    foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(400) && h.IsMelee()))
                    {
                        E.CastIfHitchanceEquals(target, HitChance.VeryHigh);
                    }
                }
            }

            if (CanHarass || (CanClear && Root.Item("harasswc").GetValue<bool>()))
            {
                if (Player.Mana / Player.MaxMana * 100 > Root.Item("qharassmana").GetValue<Slider>().Value)
                {
                    var qtarget = TargetSelector.GetTarget(525 + RocketRange + 250, TargetSelector.DamageType.Physical);
                    if (CanHarass && qtarget.IsValidTarget() == false && !minigunOut)
                    {
                        if (Root.Item("useqharass").GetValue<bool>())
                        {
                            Q.Cast();
                        }
                    }

                    if (Root.Item("useqharass").GetValue<bool>())
                    {
                        var minion = GetHarassMinion(qtarget);
                        if (minion.IsValidTarget() && IsWhiteListed(qtarget))
                        {
                            if (!minigunOut && Player.ManaPercent < Root.Item("qharassmana").GetValue<Slider>().Value)
                            {
                                Q.Cast();
                            }

                            if (!minigunOut && Player.ManaPercent > Root.Item("qharassmana").GetValue<Slider>().Value)
                            {
                                if (minion.Distance(Player.ServerPosition) <= 515 + RocketRange)
                                {
                                    Orbwalker.ForceTarget(minion);
                                }
                            }

                            if (minigunOut && minion.Distance(Player.ServerPosition) <= 515 + RocketRange)
                            {
                                Q.Cast();
                            }
                        }

                    }

                    if (Root.Item("useqharassminion").GetValue<bool>())
                    {
                        return;
                    }

                    if (qtarget.IsValidTarget() && Q.IsReady() && IsWhiteListed(qtarget))
                    {
                        if (Root.Item("useqharass").GetValue<bool>())
                        {
                            if (!minigunOut && Player.ManaPercent < Root.Item("qharassmana").GetValue<Slider>().Value)
                            {
                                Q.Cast();
                            }

                            if (minigunOut && Player.ManaPercent > Root.Item("qharassmana").GetValue<Slider>().Value)
                            {
                                if (qtarget.Distance(Player.ServerPosition) > 525)
                                    Q.Cast();
                            }

                            if (!minigunOut && qtarget.Distance(Player.ServerPosition) <= 525)
                            {
                                Q.Cast();
                            }
                        }
                    }

                    var wtarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                    if (wtarget.IsValidTarget() && W.IsReady() && IsWhiteListed(wtarget))
                    {
                        if (Root.Item("usewharass").GetValue<bool>())
                            W.CastIfHitchanceEquals(wtarget, HitChance.VeryHigh);
                    }
                }
            }

            if (CanClear && Q.IsReady())
            {
                if (Root.Item("useqclear").GetValue<bool>())
                {
                    if (Player.Mana / Player.MaxMana * 100 > Root.Item("waveclearmana").GetValue<Slider>().Value)
                    {
                        if (GetCenterMinion(Root.Item("useqclearkill").GetValue<bool>() || Player.Level > 14).IsValidTarget())
                        {
                            if (minigunOut)
                            {
                                Q.Cast();
                            }

                            if (!minigunOut && Player.Level >= 11)
                            {
                                Orbwalker.ForceTarget(GetCenterMinion(Root.Item("useqclearkill").GetValue<bool>() || Player.Level > 14));
                            }
                        }
                    }

                    if (Player.Mana / Player.MaxMana * 100 > Root.Item("jungleclearmana").GetValue<Slider>().Value)
                    {
                        if (Q.IsReady())
                        {
                            if (minigunOut)
                            {
                                var bigMob = JungleMobsInRange(525 + RocketRange).FirstOrDefault();
                                if (bigMob != null)
                                {
                                    var minionsNearMob = ObjectManager.Get<Obj_AI_Minion>().Where(x => x.Distance(bigMob) <= FarmRadius * 2);
                                    if (minionsNearMob.Count() > Root.Item("clearqmin").GetValue<Slider>().Value + 1)
                                    {
                                        Q.Cast();
                                    }
                                }
                            }
                        }                      
                    }
                }
            }

            var hasRockets = Player.GetSpell(SpellSlot.Q).ToggleState == 2;
            if (hasRockets && Q.IsReady())
            {
                if (!Root.Item("useclear").GetValue<KeyBind>().Active &&
                    !Root.Item("usecombo").GetValue<KeyBind>().Active &&
                    !Root.Item("useharass").GetValue<KeyBind>().Active)
                {
                    if (Player.ManaPercent <= 35 ||  !GetCenterMinion().IsValidTarget())
                    {
                        Q.Cast();
                    }
                }            
            }

            if (E.IsReady())
            {
                foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(E.Range)))
                {
                    if (Root.Item("autoe").GetValue<bool>())
                        E.CastIfHitchanceEquals(target, HitChance.Immobile);

                    if (Root.Item("autoedash").GetValue<bool>())
                        E.CastIfHitchanceEquals(target, HitChance.Dashing);
                }
            }

            if (Root.Item("autor").GetValue<bool>())
            {
                var maxDistance = Root.Item("maxrdist").GetValue<Slider>().Value;
                var minDistance = Root.Item("minrdist").GetValue<Slider>().Value; // 450

                foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(maxDistance)))
                {
                    var noksDistance = Math.Max(1, target.CountAlliesInRange(Math.Min(minDistance * 2, 900)) - 1) * 125;
                    var canr = target.Distance(Player.ServerPosition) > minDistance + noksDistance && !target.IsZombie &&
                               !TargetSelector.IsInvulnerable(target, TargetSelector.DamageType.Physical);

                    var aaDamage = Orbwalking.InAutoAttackRange(target)
                        ? Player.GetAutoAttackDamage(target, true)
                        : 0;

                    if (target.Health - aaDamage <= GetRDamage(target) && canr)
                    {
                        R.Cast(target);
                    }
                }
            }
        }


        internal static IEnumerable<Obj_AI_Minion> JungleMobsInRange(float range)
        {
            var names = new[]
            {
                // summoners rift
                "SRU_Razorbeak", "SRU_Krug", "Sru_Crab",
                "SRU_Baron", "SRU_Dragon", "SRU_Blue", "SRU_Red", "SRU_Murkwolf", "SRU_Gromp",

                // twisted treeline
                "TT_NGolem5", "TT_NGolem2", "TT_NWolf6", "TT_NWolf3",
                "TT_NWraith1", "TT_Spider"
            };

            var minions =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(minion => !minion.Name.Contains("Mini") && minion.IsValidTarget(range))
                    .Where(minion => names.Any(name => minion.Name.StartsWith(name)));

            return minions;
        }

        internal static bool IsWhiteListed(Obj_AI_Hero unit)
        {
            return Root.Item("hwl" + unit.ChampionName).GetValue<bool>();
        }

        internal static Obj_AI_Base GetCenterMinion(bool cankill = false)
        {
            var minions = cankill
                ? MinionManager.GetMinions(525 + RocketRange)
                    .Where(x => x.Health <= Player.GetAutoAttackDamage(x, true))
                : MinionManager.GetMinions(525 + RocketRange);

            var centerlocation =
                MinionManager.GetBestCircularFarmLocation(minions.Select(x => x.Position.To2D()).ToList(), 250,
                    525 + RocketRange);

            return centerlocation.MinionsHit >= Root.Item("clearqmin").GetValue<Slider>().Value
                ? MinionManager.GetMinions(1000).OrderBy(x => x.Distance(centerlocation.Position)).FirstOrDefault()
                : null;
        }

        internal static Obj_AI_Base GetHarassMinion(Obj_AI_Hero target)
        {
            if (target == null)
            {
                return null;
            }

            var minions = MinionManager.GetMinions(525 + RocketRange);
            foreach (var minion in minions)
            {
                if (minion.Distance(target) <= FarmRadius)
                {
                    var dmgdealtToMinion = Player.GetAutoAttackDamage(minion, true);
                    if (minion.Health - dmgdealtToMinion > 150)
                    {
                        return minion;
                    }
                }
            }

            return null;
        }

        internal static int CountInPath(
            Vector3 startpos, Vector3 endpos, 
            float width, float range,
            out List<Obj_AI_Base> units, bool minion = false)
        {
            var end = endpos.To2D();
            var start = startpos.To2D();
            var direction = (end - start).Normalized();
            var endposition = start + direction * start.Distance(endpos);

            var objinpath = from unit in ObjectManager.Get<Obj_AI_Base>().Where(b => b.Team != Player.Team)
                where Player.ServerPosition.Distance(unit.ServerPosition) <= range
                where unit is Obj_AI_Hero || unit is Obj_AI_Minion && minion
                let proj = unit.ServerPosition.To2D().ProjectOn(start, endposition)
                let projdist = unit.Distance(proj.SegmentPoint)
                where unit.BoundingRadius + width > projdist
                select unit;

            units = objinpath.ToList();
            return units.Count();
        }

        internal static float GetRDamage(Obj_AI_Hero target)
        {
            if (target == null)
            {
                return 0f;
            }

            var units = new List<Obj_AI_Base>();
            var maxdist = Player.Distance(target.ServerPosition) > 750;
            var maxrdist = Root.Item("maxrdist").GetValue<Slider>().Value;

            // impact physical damage
            var idmg = R.IsReady() &&
                       CountInPath(Player.ServerPosition, target.ServerPosition, R.Width + 225,
                           (maxrdist * 2), out units) <= 1
                ? (maxdist ? R.GetDamage(target, 1) : R.GetDamage(target, 0))
                : 0;

            // explosion damage
            var edmg = R.IsReady() &&
                       CountInPath(Player.ServerPosition, target.ServerPosition, R.Width + 225,
                           (maxrdist * 2), out units) > 1 &&
                            target.Distance(units.OrderBy(x => x.Distance(Player.ServerPosition))
                                .First(t => t.NetworkId != target.NetworkId).ServerPosition) <= R.Width + 225 // explosion radius? :^)
                ? (maxdist
                    ? (float) // maximum explosion dmage
                        (Player.CalcDamage(target, Damage.DamageType.Physical,
                            new double[] { 160, 224, 288 } [R.Level - 1] +
                            new double[] { 20, 24, 28 } [R.Level - 1] / 100 * (target.MaxHealth - target.Health) +
                            0.8 * Player.FlatPhysicalDamageMod))
                    : (float) // minimum explosion damage
                        (Player.CalcDamage(target, Damage.DamageType.Physical,
                            new double[] { 20, 28, 36 } [R.Level - 1] +
                            new double[] { 20, 24, 28 } [R.Level - 1] / 100 * (target.MaxHealth - target.Health) +
                            0.08 * Player.FlatPhysicalDamageMod)))
                : 0;

            return idmg + edmg;
        }



    }
}
