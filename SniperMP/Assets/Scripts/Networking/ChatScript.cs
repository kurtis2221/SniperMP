using UnityEngine;
using System.Collections;

public class ChatScript : MonoBehaviour {
	
	string message = "";
	GameObject stella;
	Transform tmpgz;
	
	//Check variables
	NetworkViewID tmpid;
	string tmpstr = null;
	bool isadmin = false;
	int tmpnum = 0;
	byte tmpbyte = 0;
	int bots = 0;
	GameObject[] bot = new GameObject[4];
	int[] botskin = new int[4];
	
	//Script ref
	NetScore score;
	ChatGUI gui;
	
	void Awake()
	{
		score = GetComponent<NetScore>();
		gui = GetComponent<ChatGUI>();
	}
	
	void OnGUI()
	{
		if (Event.current.Equals(Event.KeyboardEvent("return")))
		{
			if(message.Length > 0)
			{
				if(message.StartsWith("/"))
				{
					if(Network.isServer || isadmin)
					{
						if(message.StartsWith("/kick "))
						{
							tmpstr = message.Replace("/kick ","");
							if(string.Compare(tmpstr,GetComponent<NetworkMenu>().player_name,true) != 0)
							{
								if(Network.isServer)
									DoKick(tmpstr);
								else
									networkView.RPC("DoKick", RPCMode.Server, tmpstr);
							}
						}
						else if(message.StartsWith("/ban "))
						{
							tmpstr = message.Replace("/ban ","");
							if(string.Compare(tmpstr,GetComponent<NetworkMenu>().player_name,true) != 0)
							{
								if(Network.isServer)
									DoBan(tmpstr);
								else
									networkView.RPC("DoBan", RPCMode.Server, tmpstr);
							}
						}
						else if(message.StartsWith("/reloadbans"))
						{
							if(Network.isServer)
								ReloadBans();
							else
								networkView.RPC("ReloadBans", RPCMode.Server);
							gui.MessageTrigger2("Tiltólista újratöltve!");
						}
						else if(message.StartsWith("/goto "))
						{
							tmpstr = message.Replace("/goto ","");
							if(string.Compare(tmpstr,GetComponent<NetworkMenu>().player_name,true) != 0)
							{
								if(Network.isServer)
								{
									if(!byte.TryParse(tmpstr,out tmpbyte))
										tmpid = score.GetPlayerViewID(tmpstr);
									else
									{
										tmpid = score.GetPlayerViewID(tmpbyte);
										if(tmpid.isMine) goto end;
									}
									if(tmpid != default(NetworkViewID))
										GetComponent<NetworkMenu>().plobj.transform.position = NetworkView.Find(tmpid).transform.position;
								}
								else
									networkView.RPC("DoGoto",RPCMode.Server,tmpstr,GetComponent<NetworkMenu>().plobj.networkView.viewID);
							}
						}
						else if(message.StartsWith("/get "))
						{
							tmpstr = message.Replace("/get ","");
							if(string.Compare(tmpstr,GetComponent<NetworkMenu>().player_name,true) != 0)
							{
								if(Network.isServer)
								{
									if(!byte.TryParse(tmpstr,out tmpbyte))
										tmpid = score.GetPlayerViewID(tmpstr);
									else
									{
										tmpid = score.GetPlayerViewID(tmpbyte);
										if(tmpid.isMine) goto end;
									}
									if(tmpid != default(NetworkViewID))
										NetworkView.Find(tmpid).GetComponent<MainScript>().TeleportPlayer(tmpid.owner,GetComponent<NetworkMenu>().plobj.transform.position);
								}
								else
									networkView.RPC("DoGet",RPCMode.Server,tmpstr,GetComponent<NetworkMenu>().plobj.networkView.viewID);
							}
						}
						else if(message.StartsWith("/changelevel "))
						{
							int.TryParse(message.Replace("/changelevel ",""), out tmpnum);
							if(tmpnum < 1) tmpnum = 1;
							else if(tmpnum > 12) tmpnum = 12;
							if(tmpnum != Application.loadedLevel)
								GetComponent<NetworkMenu>().ChangeLevel(tmpnum);
						}
						else if(message.StartsWith("/showstella"))
						{
							if(!GameObject.Find("stella_spawn"))
								gui.MessageTrigger2("Csak nyílt pályán működik a parancs.");
							else
							{
								gui.MessageTrigger("Stella megidézve!");
								if(Network.isServer)
									ShowGodzilla();
								else
									networkView.RPC("ShowGodzilla", RPCMode.Server);
							}
						}
						else if(message.StartsWith("/addbot"))
						{
							if(Application.loadedLevel < 10)
							{
								int.TryParse(message.Replace("/addbot ",""), out tmpnum);
								
								if(tmpnum < 1) tmpnum = 1;
								else if(tmpnum > 4) tmpnum = 4;
								
								if(Network.isServer)
									SpawnBot(tmpnum);
								else
									networkView.RPC("SpawnBot", RPCMode.Server, tmpnum);
							}
							else
								gui.MessageTrigger2("A parancs nem működik bónuszpályán.");
						}
						else if(message.StartsWith("/rembot"))
						{
							if(Application.loadedLevel < 10)
							{
								if(Network.isServer)
									RemoveBot();
								else
									networkView.RPC("RemoveBot", RPCMode.Server);
							}
							else
								gui.MessageTrigger2("A parancs nem működik bónuszpályán.");
						}
						else if(message.StartsWith("/summonboss"))
						{
							if(Application.loadedLevel == 12)
							{
								int.TryParse(message.Replace("/summonboss ",""), out tmpnum);
								gui.MessageTrigger("Stella megidézve!");
								if(Network.isServer)
									SummonStella(tmpnum);
								else
									networkView.RPC("SummonStella", RPCMode.Server, tmpnum);
							}
							else
								gui.MessageTrigger2("Ezt a parancsot csak a 3. Bónuszpályán használhatod");
						}
						else if(message.StartsWith("/killboss"))
						{
							if(Application.loadedLevel == 12)
							{
								gui.MessageTrigger("Stella elpusztítva!");
								if(Network.isServer)
									DestroyStella();
								else
									networkView.RPC("DestroyStella", RPCMode.Server);
							}
							else
								gui.MessageTrigger2("Ezt a parancsot csak a 3. Bónuszpályán használhatod");
						}
						else
							gui.MessageTrigger2("HIBA: Ismeretlen parancs!");
					end:
						tmpstr = null;
						tmpid = default(NetworkViewID);
					}
					else
					{
						if(message.StartsWith("/admin "))
						{
							tmpstr = message.Replace("/admin ","");
							networkView.RPC("RequestPassword",RPCMode.Server,tmpstr);
						}
						else
							gui.MessageTrigger2("HIBA: Ismeretlen parancs!");
					}
				}
				else
					gui.MessageTrigger(GetComponent<NetworkMenu>().player_name + ": " + message);
			}
			message = "";
			enabled = false;
		}
		else if (Event.current.Equals(Event.KeyboardEvent("escape")))
		{
			message = "";
			enabled = false;
		}
		GUI.Label(new Rect(92,128,24,24),"Say:");
		GUI.SetNextControlName("ChatBox");
		message = GUI.TextField(new Rect(128,128,256,24),message,64);	
		GUI.FocusControl("ChatBox");
	}
	
	[RPC]
	void RequestPassword(string pass, NetworkMessageInfo pl)
	{
		if(CFGLoader.adminpass.Length > 0)
		{
			if(pass == CFGLoader.adminpass)
				networkView.RPC("SendReply",pl.sender,0);
			else
				GetComponent<NetworkMenu>().CallError(pl.sender, "Elrontott admin jelszó.");
		}
		else
			networkView.RPC("SendReply",pl.sender,1);
	}
	
	[RPC]
	void SendReply(int code)
	{
		if(code == 0)
		{
			gui.MessageTrigger2("Sikeresen bejelentkeztél.");
			isadmin = true;
		}
		else if(code == 1) gui.MessageTrigger2("A szerveren nincs admin jelszó.");
	}
	
	[RPC]
	void DoKick(string pname)
	{
		if(!byte.TryParse(pname,out tmpbyte))
			tmpid = score.GetPlayerViewID(pname);
		else
			tmpid = score.GetPlayerViewID(tmpbyte);
		if(tmpid != default(NetworkViewID) && !tmpid.isMine)
		{
			pname = NetworkView.Find(tmpid).GetComponent<MainScript>().player_name;
			GetComponent<NetworkMenu>().CallError(tmpid.owner,"Ki lettél rúgva a szerverről!");
			Network.CloseConnection(tmpid.owner,true);
			gui.MessageTrigger(pname + " ki lett rúgva.");
		}
		tmpid = default(NetworkViewID);
	}
	
	[RPC]
	void DoBan(string pname)
	{
		if(!byte.TryParse(pname,out tmpbyte))
			tmpid = score.GetPlayerViewID(pname);
		else
			tmpid = score.GetPlayerViewID(tmpbyte);
		if(tmpid != default(NetworkViewID) && !tmpid.isMine)
		{
			pname = NetworkView.Find(tmpid).GetComponent<MainScript>().player_name;
			NetworkMenu.AddBan(tmpid.owner.ipAddress);
			GetComponent<NetworkMenu>().CallError(tmpid.owner,"Ki lettél tiltva a szerverről!");
			Network.CloseConnection(tmpid.owner,true);
			gui.MessageTrigger(pname + " ki lett tiltva.");
		}
		tmpid = default(NetworkViewID);
	}
	
	[RPC]
	void ReloadBans()
	{
		NetworkMenu.LoadBans();
	}
	
	[RPC]
	void DoGoto(string pname, NetworkViewID id)
	{
		if(!byte.TryParse(pname,out tmpbyte))
			tmpid = score.GetPlayerViewID(pname);
		else
			tmpid = score.GetPlayerViewID(tmpbyte);
		if(tmpid.isMine && !CFGLoader.dedicated)
			NetworkView.Find(id).GetComponent<MainScript>().TeleportPlayer(id.owner,GetComponent<NetworkMenu>().plobj.transform.position);
		else if(tmpid != default(NetworkViewID))
			NetworkView.Find(id).GetComponent<MainScript>().TeleportPlayer(id.owner,NetworkView.Find(tmpid).transform.position);
		tmpid = default(NetworkViewID);
	}
	
	[RPC]
	void DoGet(string pname, NetworkViewID id)
	{
		if(!byte.TryParse(pname,out tmpbyte))
			tmpid = score.GetPlayerViewID(pname);
		else
			tmpid = score.GetPlayerViewID(tmpbyte);
		if(tmpid.isMine && !CFGLoader.dedicated)
			GetComponent<NetworkMenu>().plobj.transform.position = NetworkView.Find(id).transform.position;
		else if(tmpid != default(NetworkViewID))
			NetworkView.Find(tmpid).GetComponent<MainScript>().TeleportPlayer(tmpid.owner,NetworkView.Find(id).transform.position);
		tmpid = default(NetworkViewID);
	}
	
	[RPC]
	void SummonStella(int health)
	{
		if(stella == null)
		{
			stella = (GameObject)Network.Instantiate(Resources.Load("Boss/boss"),new Vector3(0,0,0),new Quaternion(0,0,0,0),0);
			if(health > 99)
				stella.transform.Find("stella_boss").GetComponent<BossScript>().SetBossHP(health);
		}
	}
	
	[RPC]
	void DestroyStella()
	{
		if(stella != null)
		{
			Network.RemoveRPCs(stella.networkView.viewID);
			Network.Destroy(stella);
			Destroy(stella);
			stella = null;
		}
	}
	
	[RPC]
	void ShowGodzilla()
	{
		if(stella != null)
		{
			Network.RemoveRPCs(stella.networkView.viewID);
			Network.Destroy(stella);
			Destroy(stella);
			stella = null;
		}
		tmpgz = GameObject.Find("stella_spawn").transform;
		stella = (GameObject)Network.Instantiate(Resources.Load("stellagodzilla"),tmpgz.position,tmpgz.rotation,0);
		stella.GetComponent<Godzilla>().StartMove();
	}
	
	[RPC]
	void SpawnBot(int numb)
	{
		if(bots < 4)
		{
			gui.MessageTrigger((numb-(4-bots) > 0 ? (4-bots).ToString() : numb.ToString()) + " bot megidézve");
			for(int i = 0; i < numb; i++)
			{
				if(bots < 4)
				{
				retry:
					botskin[bots] = Random.Range(1,8);
					if(bots > 0)
					{
						for(int i2 = 0; i2 < bots; i2++)
							if(botskin[i2] == botskin[bots])
								goto retry;
					}
					bot[bots] = (GameObject)Network.Instantiate(Resources.Load("Bot/bot" + botskin[bots].ToString()),new Vector3(0,0,0),new Quaternion(0,0,0,0),0);
					bots+=1;
				}
			}
		}
	}
	
	[RPC]
	void RemoveBot()
	{
		if(bots > 0)
		{
			gui.MessageTrigger("Botok törölve!");
			for(int i = 0; i < bots; i++)
			{
				Network.RemoveRPCs(bot[i].networkView.viewID);
				Network.Destroy(bot[i]);
				Destroy(bot[i]);
				bot[i] = null;
				botskin[i] = default(int);
			}
			bots = 0;
		}
	}
}