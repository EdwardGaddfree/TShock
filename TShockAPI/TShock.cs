﻿using System;
using System.IO;
using System.Net;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaAPI;
using TerrariaAPI.Hooks;

namespace TShockAPI
{
    public class TShock : TerrariaPlugin
    {
        uint[] tileThreshold = new uint[Main.maxPlayers];

        public static string saveDir = "./tshock/";

        public static int version = 10;

        public static bool shownVersion = false;

        public static string tileWhitelist = "";

        public override Version Version
        {
            get { return new Version(0, 1); }
        }

        public override Version APIVersion
        {
            get { return new Version(1, 1); }
        }

        public override string Name
        {
            get { return "TShock"; }
        }

        public override string Author
        {
            get { return "The TShock Team"; }
        }

        public override string Description
        {
            get { return "The administration modification of the future."; }
        }

        public TShock(Main game)
            : base(game)
        {
        }

        public override void Initialize()
        {
            GameHooks.OnPreInitialize += OnPreInit;
            GameHooks.OnPostInitialize += OnPostInit;
            GameHooks.OnUpdate += new Action<Microsoft.Xna.Framework.GameTime>(OnUpdate);
            GameHooks.OnLoadContent += new Action<Microsoft.Xna.Framework.Content.ContentManager>(OnLoadContent);
            ServerHooks.OnChat += new Action<int, string, HandledEventArgs>(OnChat);
            ServerHooks.OnJoin += new Action<int, AllowEventArgs>(OnJoin);
            NetHooks.OnPreGetData += GetData;
            NetHooks.OnGreetPlayer += new NetHooks.GreetPlayerD(OnGreetPlayer);
            NpcHooks.OnStrikeNpc += new NpcHooks.StrikeNpcD(NpcHooks_OnStrikeNpc);
        }

        public override void DeInitialize()
        {
            GameHooks.OnPreInitialize -= OnPreInit;
            GameHooks.OnPostInitialize -= OnPostInit;
            GameHooks.OnUpdate -= new Action<Microsoft.Xna.Framework.GameTime>(OnUpdate);
            GameHooks.OnLoadContent -= new Action<Microsoft.Xna.Framework.Content.ContentManager>(OnLoadContent);
            ServerHooks.OnChat -= new Action<int, string, HandledEventArgs>(OnChat);
            ServerHooks.OnJoin -= new Action<int, AllowEventArgs>(OnJoin);
            NetHooks.OnPreGetData -= GetData;
            NetHooks.OnGreetPlayer -= new NetHooks.GreetPlayerD(OnGreetPlayer);
            NpcHooks.OnStrikeNpc -= new NpcHooks.StrikeNpcD(NpcHooks_OnStrikeNpc);
        }

        /*
         * Hooks:
         * */

        void NpcHooks_OnStrikeNpc(NpcStrikeEventArgs e)
        {
            if (ConfigurationManager.infiniteInvasion)
            {
                IncrementKills();
                if (Main.invasionSize < 10)
                {
                    Main.invasionSize = 20000000;
                }
            }
        }

        void OnPreGetData(byte id, messageBuffer msg, int idx, int length, HandledEventArgs e)
        {
            if (Main.netMode != 2) { return; }
            if (id == 0x1e && ConfigurationManager.permaPvp)
            {
                e.Handled = true;
            }
        }

        void GetData(GetDataEventArgs e)
        {
            if (Main.netMode != 2) { return; }
            if (e.MsgID == 17)
            {
                byte type;
                int x = 0;
                int y = 0;
                using (var br = new BinaryReader(new MemoryStream(e.Msg.readBuffer, e.Index, e.Length)))
                {
                    type = br.ReadByte();
                    x = br.ReadInt32();
                    y = br.ReadInt32();
                }
                if (type == 0 && Main.tileSolid[Main.tile[x, y].type] && Main.player[e.Msg.whoAmI].active)
                {
                    tileThreshold[e.Msg.whoAmI]++;
                }
                return;
            }
            if (e.MsgID == 0x1e)
            {
                Main.player[e.Msg.whoAmI].hostile = true;
                NetMessage.SendData(30, -1, -1, "", e.Msg.whoAmI);
                e.Handled = true;
            }
        }

        void OnGreetPlayer(int who, HandledEventArgs e)
        {
            if (Main.netMode != 2) { return; }
            int plr = who; //legacy support
            ShowUpdateReminder(who);
            Tools.ShowMOTD(who);
            if (Main.player[plr].statLifeMax > 400 || Main.player[plr].statManaMax > 200 || Main.player[plr].statLife > 400 || Main.player[plr].statMana > 200)
            {
                Tools.HandleCheater(plr);
            }
            if (ConfigurationManager.permaPvp)
            {
                Main.player[who].hostile = true;
                NetMessage.SendData(30, -1, -1, "", who);
            }
            if (Tools.IsAdmin(who) && ConfigurationManager.infiniteInvasion && !ConfigurationManager.startedInvasion)
            {
                StartInvasion();
            }
            e.Handled = true;
        }

        void OnChat(int ply, string msg, HandledEventArgs handler)
        {
            if (Main.netMode != 2) { return; }
            int x = (int)Main.player[ply].position.X;
            int y = (int)Main.player[ply].position.Y;

            if (Tools.IsAdmin(ply))
            {
                if (msg.Length > 5 && msg.Substring(0, 5) == "/kick")
                {
                    string plStr = msg.Remove(0, 5).Trim();
                    if (!(Tools.FindPlayer(plStr) == -1 || plStr == ""))
                    {
                        Tools.Kick(Tools.FindPlayer(plStr), "You were kicked.");
                        Tools.Broadcast(plStr + " was kicked by " + Tools.FindPlayer(ply));
                    }
                    handler.Handled = true;
                }

                if (msg.Length > 4 && msg.Substring(0, 4) == "/ban")
                {
                    string plStr = msg.Remove(0, 4).Trim();
                    if (!(Tools.FindPlayer(plStr) == -1 || plStr == ""))
                    {
                        FileTools.WriteBan(Tools.FindPlayer(plStr));
                        Tools.Kick(Tools.FindPlayer(plStr), "You were banned.");
                    }
                    handler.Handled = true;
                }

                if (msg == "/off")
                {
                    Netplay.disconnect = true;
                    handler.Handled = true;
                }

                if (msg == "/reload")
                {
                    FileTools.SetupConfig();
                    handler.Handled = true;
                }

                if (msg == "/dropmeteor")
                {
                    WorldGen.spawnMeteor = false;
                    WorldGen.dropMeteor();
                    handler.Handled = true;
                }

                if (msg == "/star")
                {
                    int penis56 = 12;
                    int penis57 = Main.rand.Next(Main.maxTilesX - 50) + 100;
                    penis57 *= 0x10;
                    int penis58 = Main.rand.Next((int)(Main.maxTilesY * 0.05)) * 0x10;
                    Microsoft.Xna.Framework.Vector2 vector = new Microsoft.Xna.Framework.Vector2((float)penis57, (float)penis58);
                    float speedX = Main.rand.Next(-100, 0x65);
                    float speedY = Main.rand.Next(200) + 100;
                    float penis61 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                    penis61 = ((float)penis56) / penis61;
                    speedX *= penis61;
                    speedY *= penis61;
                    Projectile.NewProjectile(vector.X, vector.Y, speedX, speedY, 12, 0x3e8, 10f, Main.myPlayer);
                    handler.Handled = true;
                }
                if (msg == "/bloodmoon")
                {
                    Tools.Broadcast(Tools.FindPlayer(ply) + " turned on blood moon.");
                    Main.bloodMoon = true;
                    Main.time = 0;
                    Main.dayTime = false;
                    //Main.UpdateT();
                    NetMessage.SendData(18, -1, -1, "", 0, 0, Main.sunModY, Main.moonModY);
                    NetMessage.syncPlayers();
                    handler.Handled = true;
                    handler.Handled = true;
                }
                if (msg == "/eater")
                {
                    Tools.NewNPC((int)ConfigurationManager.NPCList.WORLD_EATER, x, y, ply);
                    Tools.Broadcast(Tools.FindPlayer(ply) + " has spawned an eater of worlds!");
                    handler.Handled = true;
                }
                if (msg == "/eye")
                {
                    Tools.NewNPC((int)ConfigurationManager.NPCList.EYE, x, y, ply);
                    Tools.Broadcast(Tools.FindPlayer(ply) + " has spawned an eye!");
                    handler.Handled = true;
                }
                if (msg == "/skeletron")
                {
                    Tools.NewNPC((int)ConfigurationManager.NPCList.SKELETRON, x, y, ply);
                    Tools.Broadcast(Tools.FindPlayer(ply) + " has spawned skeletron!");
                    handler.Handled = true;
                }
                if (msg == "/hardcore")
                {
                    for (int i = 0; i <= 2; i++)
                    {
                        Tools.NewNPC(i, x, y, ply);
                    }
                    Tools.Broadcast(Tools.FindPlayer(ply) + " has spawned all 3 bosses!");
                    handler.Handled = true;
                }
                if (msg == "/invade")
                {
                    Tools.Broadcast(Main.player[ply].name + " started an invasion.");
                    StartInvasion();
                    handler.Handled = true;
                }
                if (msg.Length > 9 && msg.Substring(0, 9) == "/password")
                {
                    string passwd = msg.Remove(0, 9).Trim();
                    Netplay.password = passwd;
                    Tools.SendMessage(ply, "Server password changed to: " + passwd);
                    handler.Handled = true;
                }
                if (msg == "/save")
                {
                    WorldGen.saveWorld();
                    Tools.SendMessage(ply, "World saved.");
                    handler.Handled = true;
                }
                if (msg == "/spawn")
                {
                    Teleport(ply, Main.player[ply].SpawnX * 16, Main.player[ply].SpawnY * 16);
                    Tools.SendMessage(ply, "Teleported to your spawnpoint.");
                    handler.Handled = true;
                }
                if (msg.Length > 3 && msg.Substring(0, 3) == "/tp")
                {
                    string player = msg.Remove(0, 3).Trim();
                    if (!(Tools.FindPlayer(player) == -1) && !(player == ""))
                        if (Tools.FindPlayer(player) != -1 && player != "")
                        {
                            Teleport(ply, Main.player[Tools.FindPlayer(player)].position.X, Main.player[Tools.FindPlayer(player)].position.Y);
                            Tools.SendMessage(ply, "Teleported to " + player);
                            handler.Handled = true;
                        }
                }
                if (msg.Length > 7 && msg.Substring(0, 7) == "/tphere")
                {
                    string player = msg.Remove(0, 7).Trim();
                    if (!(Tools.FindPlayer(player) == -1) && !(player == ""))
                        if (Tools.FindPlayer(player) != -1 && player != "")
                        {
                            Teleport(Tools.FindPlayer(player), Main.player[ply].position.X, Main.player[ply].position.Y);
                            Tools.SendMessage(Tools.FindPlayer(player), "You were teleported to " + Tools.FindPlayer(ply) + ".");
                            Tools.SendMessage(ply, "You brought " + player + " here.");
                            handler.Handled = true;
                        }
                }
                if (msg.Length > 9 && msg.Substring(0, 9) == "/spawnmob")
                {
                    string args = msg.Remove(0, 9).Trim();
                    int type = 0;
                    if (int.TryParse(args, out type))
                    {
                        if (type >= 1 && type <= 43)
                        {
                            var npcid = NPC.NewNPC(x, y, type, 0);
                            Tools.Broadcast("NPC " + type.ToString() + " spawned with ID " + npcid.ToString());
                            handler.Handled = true;
                        }
                    }
                }
                //ATM it just drops the item. Trying to find out can I update the player's inventory directly.
                if (msg.StartsWith("/item") && msg.Split(' ').Length == 2) 
                {
                    var args = msg.Split(' ')[1];
                    int type = 0;
                    if (int.TryParse(args, out type))
                    {
                        if (type >= 1 && type <= 235)
                        {
                            int id = Item.NewItem(0, 0, 0, 0, type, 1, true);
                            Main.item[id].position.X = (float)x;
                            Main.item[id].position.Y = (float)y;
                            Main.item[id].stack = Main.item[id].maxStack;
                            NetMessage.SendData(21, -1, -1, "", id, 0f, 0f, 0f);
                            Tools.SendMessage(ply, "Spawned " + Main.item[id].name + ".");
                            handler.Handled = true;
                        }
                    }
                }
            }
            if (msg == "/help")
            {
                Tools.SendMessage(ply, "TShock Commands:");
                Tools.SendMessage(ply, "/kick, /ban, /reload, /off, /dropmeteor, /invade");
                Tools.SendMessage(ply, "/star, /skeletron, /eye, /eater, /hardcore");
                Tools.SendMessage(ply, "Terraria commands:");
                Tools.SendMessage(ply, "/playing, /p, /me");
                handler.Handled = true;
            }
        }

        void OnJoin(int ply, AllowEventArgs handler)
        {
            if (Main.netMode != 2) { return; }
            string ip = Tools.GetRealIP((Convert.ToString(Netplay.serverSock[ply].tcpClient.Client.RemoteEndPoint)));
            if (FileTools.CheckBanned(ip) || FileTools.CheckCheat(ip) || FileTools.CheckGreif(ip))
            {
                Tools.Kick(ply, "You are banned.");
            }
            if (!FileTools.OnWhitelist(ip))
            {
                Tools.Kick(ply, "Not on whitelist.");
            }
        }

        void OnLoadContent(Microsoft.Xna.Framework.Content.ContentManager obj)
        {
        }

        void OnPreInit()
        {
            FileTools.SetupConfig();
        }

        void OnPostInit()
        {
        }

        void OnUpdate(GameTime time)
        {
            if (Main.netMode != 2) { return; }
            for (uint i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active == false) { continue; }
                if (tileThreshold[i] >= 5)
                {
                    if (Main.player[i] != null)
                    {
                        FileTools.WriteGrief((int)i);
                        Tools.Kick((int)i, "Kill tile abuse detected.");
                    }
                    tileThreshold[i] = 0;
                }
                else if (tileThreshold[i] > 0)
                {
                    tileThreshold[i] = 0;
                }
            }
        }

        /*
         * Useful stuff:
         * */

        public static void ShowUpdateReminder(int ply)
        {
            if (!shownVersion)
            {
                if (Tools.IsAdmin(Tools.FindPlayer(ply)))
                {
                    WebClient client = new WebClient();
                    client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.0.3705;)");
                    try
                    {
                        string updateVersion = client.DownloadString("http://shankshock.com/tshock.txt");
                        string[] changes = updateVersion.Split(',');
                        float[] color = { 255, 255, 000 };
                        if (Convert.ToDouble(changes[0]) > version)
                        {
                            Tools.SendMessage(ply, "This server is out of date.");
                            for (int i = 1; i <= changes.Length; i++)
                            {
                                Tools.SendMessage(ply, changes[i], color);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        FileTools.WriteError(e.Message);
                    }
                    shownVersion = true;
                }
            }
        }

        public static void Teleport(int ply, int x, int y)
        {
            Main.player[ply].velocity = new Vector2(0, 0);
            NetMessage.SendData(0x0d, -1, -1, "", ply);
            Main.player[ply].position.X = x;
            Main.player[ply].position.Y = y - 0x2a;
            NetMessage.SendData(0x0d, -1, -1, "", ply);
        }

        public static void Teleport(int ply, float x, float y)
        {
            Main.player[ply].position.X = x;
            Main.player[ply].position.Y = y - 0x2a;
            NetMessage.SendData(0x0d, -1, -1, "", ply);
            int oldx = Main.player[ply].SpawnX;
            int oldy = Main.player[ply].SpawnY;
            Main.player[ply].SpawnX = (int)(x / 16);
            Main.player[ply].SpawnY = (int)((y - 0x2a) / 16);
            NetMessage.SendData(0xC, -1, -1, "", ply);
            Main.player[ply].SpawnX = oldx;
            Main.player[ply].SpawnY = oldy;
        }

        public static void StartInvasion()
        {
            Main.invasionType = 1;
            if (ConfigurationManager.infiniteInvasion)
            {
                Main.invasionSize = 20000000;
            }
            else
            {
                Main.invasionSize = 100 + (ConfigurationManager.invasionMultiplier * Tools.activePlayers());
            }

            Main.invasionWarn = 0;
            if (new Random().Next(2) == 0)
            {
                Main.invasionX = 0.0;
            }
            else
            {
                Main.invasionX = Main.maxTilesX;
            }
        }

        public static void IncrementKills()
        {
            ConfigurationManager.killCount++;
            Random r = new Random();
            int random = r.Next(5);
            if (ConfigurationManager.killCount % 100 == 0)
            {
                switch (random)
                {
                    case 0:
                        Tools.Broadcast("You call that a lot? " + ConfigurationManager.killCount + " goblins killed!");
                        break;
                    case 1:
                        Tools.Broadcast("Fatality! " + ConfigurationManager.killCount + " goblins killed!");
                        break;
                    case 2:
                        Tools.Broadcast("Number of 'noobs' killed to date: " + ConfigurationManager.killCount);
                        break;
                    case 3:
                        Tools.Broadcast("Duke Nukem would be proud. " + ConfigurationManager.killCount + " goblins killed.");
                        break;
                    case 4:
                        Tools.Broadcast("You call that a lot? " + ConfigurationManager.killCount + " goblins killed!");
                        break;
                    case 5:
                        Tools.Broadcast(ConfigurationManager.killCount + " copies of Call of Duty smashed.");
                        break;
                }
            }
        }
    }
}