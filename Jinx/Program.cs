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
        internal static float FarmRadius => Items.HasItem(3085) ? 250f: 100f;
        internal static Obj_AI_Hero Player => ObjectManager.Player;
        internal static HpBarIndicator BarIndicator = new HpBarIndicator();
        private static float windUpDist;

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

            W = new Spell(SpellSlot.W, 1500f) { MinHitChance = HitChance.VeryHigh };
            W.SetSkillshot(0.6f, 60f, 3300f, true, SkillshotType.SkillshotLine);

            E = new Spell(SpellSlot.E, 900f);
            E.SetSkillshot(1.2f, 120f, 2300f, false, SkillshotType.SkillshotCircle);

            R = new Spell(SpellSlot.R, 3000) { MinHitChance = HitChance.VeryHigh };
            R.SetSkillshot(0.6f, 140f, 1700f, false, SkillshotType.SkillshotLine);

            Root = new Menu("Jinx#", "jinx", true);

            var ormenu = new Menu("-] Orbwalk", "ormenu");
            Orbwalker = new Orbwalking.Orbwalker(ormenu);
            Root.AddSubMenu(ormenu);

            var kemenu = new Menu("-] Keys", "kemenu");
            kemenu.AddItem(new MenuItem("usecombo", "Combo [active]")).SetValue(new KeyBind(32, KeyBindType.Press));
            kemenu.AddItem(new MenuItem("useharass", "Harass [active]")).SetValue(new KeyBind('G', KeyBindType.Press));
            kemenu.AddItem(new MenuItem("useclear", "Wave/Jungle [active]")).SetValue(new KeyBind(86, KeyBindType.Press));
            kemenu.AddItem(new MenuItem("useflee", "Flee [active]")).SetValue(new KeyBind('A', KeyBindType.Press));
            Root.AddSubMenu(kemenu);

            var comenu = new Menu("-] Combo", "cmenu");

            var tcmenu = new Menu("-] Misc", "tcmenu");

            tcmenu.AddItem(new MenuItem("autor", "Auto R Killable")).SetValue(true);
            tcmenu.AddItem(new MenuItem("autoe", "Auto E Immobile")).SetValue(true);
            //tcmenu.AddItem(new MenuItem("autoespell", "Auto E on Spell")).SetValue(false).SetTooltip("Use at own risk!");
            tcmenu.AddItem(new MenuItem("autoedash", "Auto E Dashing")).SetValue(true);
            tcmenu.AddItem(new MenuItem("autoegap", "Auto E Gapcloser")).SetValue(true);
            tcmenu.AddItem(new MenuItem("autoetp", "Auto E Teleport"))
                .SetValue(false).ValueChanged += (sender, eventArgs) => eventArgs.Process = false;
            comenu.AddSubMenu(tcmenu);

            Root.AddSubMenu(comenu);

            var abmenu = new Menu("-] Skills", "abmenu");
            abmenu.AddItem(new MenuItem("useqcombo", "Use Q")).SetValue(true);
            abmenu.AddItem(new MenuItem("useqcombominion", "-> Q on Minion?")).SetValue(false);
            abmenu.AddItem(new MenuItem("usewcombo", "Use W")).SetValue(true);
            abmenu.AddItem(new MenuItem("usercombo", "Use R")).SetValue(true);
            comenu.AddSubMenu(abmenu);

            var hamenu = new Menu("-] Harass", "hamenu");

            var wList = new Menu("-] Harass Whitelist", "hwl");
            foreach (var enemy in HeroManager.Enemies)
            {
                wList.AddItem(new MenuItem("hwl" + enemy.ChampionName, enemy.ChampionName))
                    .SetValue(true);
            }

            hamenu.AddSubMenu(wList);
            hamenu.AddItem(new MenuItem("useqharass", "Use Q")).SetValue(true);
            hamenu.AddItem(new MenuItem("useqharassminion", "-> Only On Minion")).SetValue(false)
                .SetTooltip("Uses Rockets on minions near Enemies so you dont get creep aggro.");
            hamenu.AddItem(new MenuItem("qharassmana", "-> Minimum mana %")).SetValue(new Slider(65));
            hamenu.AddItem(new MenuItem("usewharass", "Use W")).SetValue(true);
            hamenu.AddItem(new MenuItem("wharassmana", "-> Minimum mana %")).SetValue(new Slider(70));
            hamenu.AddItem(new MenuItem("autoqharass", "Auto Q Minion Harass").SetValue(true))
                .SetTooltip("Uses Rockets on minions near Enemies so you dont get creep aggro.");
            hamenu.AddItem(new MenuItem("autoqharassmana", "-> Minimum mana %")).SetValue(new Slider(45));

            Root.AddSubMenu(hamenu);

            var wMenu = new Menu("-] Farming", "farming");
            wMenu.AddItem(new MenuItem("useqclear", "Use Q").SetValue(true));
            wMenu.AddItem(new MenuItem("swapbackfarm", "Auto Swap to Minigun")).SetValue(true);
            wMenu.AddItem(new MenuItem("clearqmin", "Minimum minion count")).SetValue(new Slider(3, 2, 6));
            wMenu.AddItem(new MenuItem("waveclearmana", "Wave Minimum mana %")).SetValue(new Slider(75));
            wMenu.AddItem(new MenuItem("useqclearkill", "-> Or if Will Kill")).SetValue(true);
            wMenu.AddItem(new MenuItem("jungleclearmana", "Jungle Minimum mana %")).SetValue(new Slider(35));
            Root.AddSubMenu(wMenu);

            var fmenu = new Menu("-] Flee", "fmenu");
            Root.AddSubMenu(fmenu);

            var exmenu = new Menu("-] Extra", "exmenu");
            exmenu.AddItem(new MenuItem("harasswc", "Harass in Wave Clear")).SetValue(true);
            exmenu.AddItem(new MenuItem("minrdist", "Min R Distance")).SetValue(new Slider(450, 0, 3000));
            exmenu.AddItem(new MenuItem("maxrdist", "Max R distance")).SetValue(new Slider(1850, 0, 3000));
            exmenu.AddItem(new MenuItem("autoswap", "Auto Swap to Minigun when Idle")).SetValue(true);
            //exmenu.AddItem(new MenuItem("interrupt", "Interrupter")).SetValue(false);
            //exmenu.AddItem(new MenuItem("gap", "Anti-Gapcloser")).SetValue(false);
            Root.AddSubMenu(exmenu);

            var skmenu = new Menu("-] Skins", "skmenu");
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

            var drmenu = new Menu("-] Draw", "drmenu");
            drmenu.AddItem(new MenuItem("drawhpbarfill", "Draw R HPBarFill")).SetValue(true);
            drmenu.AddItem(new MenuItem("drawmyw", "Draw W")).SetValue(new Circle(true, System.Drawing.Color.FromArgb(165, 37, 230, 255)));
            drmenu.AddItem(new MenuItem("drawmyq", "Draw Q")).SetValue(new Circle(true, System.Drawing.Color.FromArgb(165, 0, 220, 144)));
            Root.AddSubMenu(drmenu);

            Root.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            //Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            Game.OnUpdate += Game_OnUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;

            var color = System.Drawing.Color.FromArgb(200, 0, 220, 144);
            var hexargb = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

            Game.PrintChat("<b><font color=\"" + hexargb + "\">Jinx#</font></b> - Loaded!");
        }

        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (Root.Item("drawhpbarfill").GetValue<bool>())
            {
                foreach (
                    var enemy in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(ene => ene.IsValidTarget() && !ene.IsZombie))
                {
                    var color = new ColorBGRA(255, 255, 0, 90);

                    BarIndicator.unit = enemy;
                    BarIndicator.drawDmg(GetRDamage(enemy), color);
                }
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!E.IsReady())
            {
                return;
            }

            if (gapcloser.Sender.IsValidTarget(300) && Root.Item("autogap").GetValue<bool>())
            {
                var castPos = gapcloser.End;
                E.Cast(castPos);
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var wCircle = Root.Item("drawmyw").GetValue<Circle>();
            if (wCircle.Active)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, wCircle.Color);
            }

            var qCircle = Root.Item("drawmyq").GetValue<Circle>();
            if (qCircle.Active)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range + windUpDist, qCircle.Color);
            }
        }

        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            // todo:
            if (sender.IsMe && args.SData.IsAutoAttack())
            {
                int settime = 5000; // 5 seconds
                var casttime = (sender.Spellbook.CastTime - Game.Time) * 1000;
                var aatime = Math.Abs(casttime - sender.AttackCastDelay * 1000);
                var aaacountinsettime = (int) (settime / aatime);

                //Console.WriteLine("Delay: " + sender.AttackCastDelay * 1000);
                //Console.WriteLine("CastTime: " + aatime);
                //Console.WriteLine("CanAA " + aaacountinsettime + " times in 5 seconds");
            }
        }

        private static float MiniGunDamageOverTime(Obj_AI_Hero sender, Obj_AI_Hero target, int time)
        {
            var casttime = (sender.Spellbook.CastTime - Game.Time) * 1000;
            var aatime = Math.Abs(casttime - sender.AttackCastDelay * 1000);
            var aaacountinsettime = (int) (time / aatime);

            var minigunDmg = sender.GetAutoAttackDamage(target, false) * aaacountinsettime;
            return (float) minigunDmg;
        }

        private static float RocketDamageOverTime(Obj_AI_Hero sender, Obj_AI_Hero target, int time)
        {
            var casttime = (sender.Spellbook.CastTime - Game.Time) * 1000;
            var aatime = Math.Abs(casttime - sender.AttackCastDelay * 1000);
            var aaacountinsettime = (int)(time / aatime);

            var minigunDmg = sender.GetAutoAttackDamage(target, true) * aaacountinsettime;
            return (float) minigunDmg;
        }

        private static float WalkDistTime(Obj_AI_Base unit)
        {
            return (1000 * (Player.Distance(unit) / Player.MoveSpeed)) + Game.Ping;
        }

        private static float WalkDistTime(Vector2 pos, float movespeed)
        {
            return (1000 * (Player.Distance(pos) / movespeed)) + Game.Ping;
        }

        private static float QSwapTime(float extraWindUp, bool toRockets = false)
        {
            var realWindUp = (1 / Player.AttackDelay) * 1000;
            float delay = 0f;

            if (toRockets)
            {
                delay += 250f;
                realWindUp += (float) (realWindUp * 0.25);
            }

            return delay + realWindUp + extraWindUp;
        }

        private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            var aiMinion = args.Target as Obj_AI_Minion;
            var hasRockets = Player.GetSpell(SpellSlot.Q).ToggleState == 1;

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
                        if (aiMinion.Distance(enemy.Position) <= FarmRadius)
                        {
                            if (!hasRockets)
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
                    if (aiMinion.Distance(enemy.Position) <= FarmRadius)
                    {
                        if (!hasRockets)
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
                if (aiHero.Distance(Player.ServerPosition) > 525 + RocketRange)
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
            RocketRange = new[] { 75, 75, 100, 125, 150, 175 } [Q.Level];

            if (Root.Item("useskin").GetValue<bool>())
            {
                Player.SetSkin(Player.CharData.BaseSkinName, Root.Item("skinid").GetValue<Slider>().Value);
            }

            if (Root.Item("useflee").GetValue<KeyBind>().Active)
            {
                Orbwalking.Orbwalk(null, Game.CursorPos);
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

            var hasRockets = Player.GetSpell(SpellSlot.Q).ToggleState == 2;
            windUpDist = Math.Max(0, QSwapTime(Game.Ping, !hasRockets) / 10);

            if (hasRockets && CanClear)
            {
                if (Root.Item("useqclear").GetValue<bool>() && Root.Item("swapbackfarm").GetValue<bool>())
                {
                    if (GetCenterMinion(false, true) == null)
                    {
                        Q.Cast();
                    }
                }
            }

            if (CanCombo)
            {
                if (hasRockets && Root.Item("useqcombo").GetValue<bool>())
                {
                    if (Player.ManaPercent < 35 &&
                        HeroManager.Enemies.Any(
                            i => i.IsValidTarget(590 + RocketRange + 10) &&
                                    Player.GetAutoAttackDamage(i, true) * 3 < i.Health))
                    {
                        Q.Cast();
                    }
                }

                var qtarget = TargetSelector.GetTarget(525 + RocketRange + windUpDist, TargetSelector.DamageType.Physical);
                if (qtarget.IsValidTarget() && Q.IsReady())
                {
                    if (Root.Item("useqcombo").GetValue<bool>())
                    {
                        if (!hasRockets && (Player.ManaPercent > 35 || Player.GetAutoAttackDamage(qtarget, true) * 3 > qtarget.Health))
                        {
                            if (qtarget.Distance(Player.ServerPosition) > 525 + windUpDist)
                            {
                                if (WalkDistTime(qtarget) > QSwapTime(Game.Ping, true))
                                {
                                    if (GetHarassObj(qtarget).IsValidTarget() && Root.Item("useqcombominion").GetValue<bool>())
                                    {
                                        Orbwalker.ForceTarget(GetHarassObj(qtarget));
                                        Orbwalking.Orbwalk(GetHarassObj(qtarget), Game.CursorPos);
                                    }

                                    Q.Cast();
                                }
                            }
                        }

                        if (hasRockets && qtarget.Distance(Player) <= 525 + windUpDist)
                        {
                            Q.Cast();
                        }

                        if (hasRockets && Player.ManaPercent < 35 && Player.GetAutoAttackDamage(qtarget, true) * 3 < qtarget.Health)
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
                        if (Player.ManaPercent > 25 || W.GetDamage(wtarget) + Player.GetAutoAttackDamage(wtarget, true) > wtarget.Health)
                        {
                            if (wtarget.Distance(Player.ServerPosition) > 525 + RocketRange)
                            {
                                if (!(Player.GetAutoAttackDamage(wtarget, true) * 2 > wtarget.Health) ||
                                    !Orbwalking.InAutoAttackRange(wtarget))
                                {
                                    W.Cast(wtarget);
                                }
                            }
                        }
                    }
                }
            }

            if (CanHarass || (CanClear && Root.Item("harasswc").GetValue<bool>()))
            {
                if (Player.Mana / Player.MaxMana * 100 > Root.Item("qharassmana").GetValue<Slider>().Value)
                {
                    var qtarget = TargetSelector.GetTarget(525 + RocketRange + 250, TargetSelector.DamageType.Physical);
                    if (CanHarass && qtarget.IsValidTarget() == false && hasRockets)
                    {
                        if (Root.Item("useqharass").GetValue<bool>())
                        {
                            Q.Cast();
                        }
                    }

                    if (Root.Item("useqharass").GetValue<bool>())
                    {
                        var minion = GetHarassObj(qtarget);
                        if (minion.IsValidTarget() && IsWhiteListed(qtarget))
                        {
                            if (hasRockets && Player.ManaPercent < Root.Item("qharassmana").GetValue<Slider>().Value)
                            {
                                Q.Cast();
                            }

                            if (hasRockets && Player.ManaPercent > Root.Item("qharassmana").GetValue<Slider>().Value)
                            {
                                if (minion.Distance(Player.ServerPosition) <= 515 + RocketRange)
                                {
                                    if (minion.Distance(Player.ServerPosition) <= 525 + RocketRange)
                                    {
                                        Orbwalker.ForceTarget(minion);
                                        Orbwalking.Orbwalk(minion, Game.CursorPos);
                                    }
                                }
                            }

                            if (!hasRockets && minion.Distance(Player.ServerPosition) <= 515 + RocketRange)
                            {
                                Q.Cast();
                            }
                        }

                    }

                    if (!Root.Item("useqharassminion").GetValue<bool>())
                    {
                        if (qtarget.IsValidTarget() && Q.IsReady() && IsWhiteListed(qtarget))
                        {
                            if (Root.Item("useqharass").GetValue<bool>())
                            {
                                if (hasRockets && Player.ManaPercent < Root.Item("qharassmana").GetValue<Slider>().Value)
                                {
                                    Q.Cast();
                                }

                                if (!hasRockets &&
                                    Player.ManaPercent > Root.Item("qharassmana").GetValue<Slider>().Value)
                                {
                                    if (qtarget.Distance(Player.ServerPosition) > 525)
                                        Q.Cast();
                                }

                                if (hasRockets && qtarget.Distance(Player.ServerPosition) <= 525)
                                {
                                    Q.Cast();
                                }
                            }
                        }

                        var wtarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                        if (wtarget.IsValidTarget() && W.IsReady() && IsWhiteListed(wtarget))
                        {
                            if (Root.Item("usewharass").GetValue<bool>())
                                W.Cast(wtarget);
                        }
                    }
                }
            }

            if (CanClear && Q.IsReady())
            {
                if (Root.Item("useqclear").GetValue<bool>())
                {
                    if (GetCenterMinion(Root.Item("useqclearkill").GetValue<bool>()) != null)
                    {
                        if (!hasRockets)
                        {
                            Q.Cast();
                        }

                        if (hasRockets)
                        {
                            Orbwalker.ForceTarget(GetCenterMinion(Root.Item("useqclearkill").GetValue<bool>()));
                        }
                    }

                    if (Player.Mana / Player.MaxMana * 100 > Root.Item("jungleclearmana").GetValue<Slider>().Value)
                    {
                        if (Q.IsReady())
                        {
                            if (!hasRockets)
                            {
                                var bigMob = JungleMobsInRange(525 + RocketRange).FirstOrDefault();
                                if (bigMob != null)
                                {
                                    var minionsNearMob = ObjectManager.Get<Obj_AI_Minion>().Where(x => x.Distance(bigMob) <= FarmRadius * 2);
                                    if (minionsNearMob.Count() > Root.Item("clearqmin").GetValue<Slider>().Value)
                                    {
                                        Q.Cast();
                                    }
                                }
                            }
                        }                      
                    }
                }
            }

            if (hasRockets && Q.IsReady() && Root.Item("autoswap").GetValue<bool>())
            {
                if (!CanClear && !CanCombo && !CanHarass)
                {
                    if (Player.ManaPercent <= 35 || !GetCenterMinion().IsValidTarget())
                    {
                        Q.Cast();
                    }
                }            
            }

            if (Root.Item("autor").GetValue<bool>() || CanCombo && Root.Item("usercombo").GetValue<bool>())
            {
                var maxDistance = Root.Item("maxrdist").GetValue<Slider>().Value;

                foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(maxDistance)))
                {
                    if (target.Health <= GetRDamage(target) * 0.958 && CanRCheck(target))
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
                // "SRU_Baron", "SRU_Dragon",
                "SRU_Blue", "SRU_Red", "SRU_Murkwolf", "SRU_Gromp",

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

        internal static Obj_AI_Base GetCenterMinion(bool willkill = false, bool combo = false)
        {
            if (!combo && (Player.Mana / Player.MaxMana * 100 > Root.Item("waveclearmana").GetValue<Slider>().Value || willkill))
            {
                var autocount = new[] { 1, 2, 3, 3 };
                var aa = autocount[Math.Min(18, Player.Level) / 6];

                var minions = MinionManager.GetMinions(525 + RocketRange);

                var centerlocation =
                    MinionManager.GetBestCircularFarmLocation(minions.Select(x => x.Position.To2D()).ToList(), FarmRadius,
                        525 + RocketRange);

                if (centerlocation.MinionsHit >= Root.Item("clearqmin").GetValue<Slider>().Value)
                {
                    var m = minions.OrderBy(x => x.Distance(centerlocation.Position)).FirstOrDefault();

                    if (willkill && Player.GetAutoAttackDamage(m, true) * aa > m?.Health || !willkill)
                    {
                        return m;
                    }
                }
            }

            return null;
        }

        internal static bool CanRCheck(Obj_AI_Hero unit)
        {
            if (unit.IsZombie || TargetSelector.IsInvulnerable(unit, TargetSelector.DamageType.Physical))
            {
                return false;
            }

            if (unit.Distance(Player.ServerPosition) < Root.Item("minrdist").GetValue<Slider>().Value)
            {
                return false;
            }

            if (unit.Distance(Player) <= W.Range && W.IsReady() && W.GetDamage(unit) >= unit.Health)
            {
                if (Root.Item("usewcombo").GetValue<bool>())
                    return false;
            }

            if (Orbwalking.InAutoAttackRange(unit))
            {
                if (Player.GetAutoAttackDamage(unit, true) >= unit.Health)
                {
                    return false;
                }
            }

            foreach (var ally in HeroManager.Allies.Where(x => !x.IsMe))
            {
                if (ally.Distance(unit.ServerPosition) <= 500 && ally.HealthPercent > unit.HealthPercent &&
                    ally.HealthPercent > 20 && unit.CountEnemiesInRange(R.Width + 250) < 2)
                {
                    return false;
                }
            }

            return true;
        }

        internal static Obj_AI_Base GetHarassObj(Obj_AI_Hero target)
        {
            if (target == null)
            {
                return null;
            }

            var objs = MinionManager.GetMinions(525 + RocketRange);
            foreach (var minion in objs.OrderBy(m => m.Distance(target.Position)))
            {
                var mPos = Prediction.GetPrediction(target, 100 + Game.Ping / 2f).UnitPosition;
                if (minion.Distance(mPos) <= FarmRadius)
                {
                    //var dmgdealtToMinion = Player.GetAutoAttackDamage(minion, true);
                    //if (minion.Health - dmgdealtToMinion > 150)
                    //{
                        return minion;
                    //}
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
