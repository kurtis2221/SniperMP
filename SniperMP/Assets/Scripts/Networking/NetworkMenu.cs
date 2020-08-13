using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class NetworkMenu : MonoBehaviour
{
	const string file_addr = "lastaddr.txt";
	const string file_bans = "bans.txt";
	
	Object[] PlayerPrefab;
	bool connected = false;
	static bool isloaded = false;
	static string ip = "127.0.0.1";
	Transform[] spawns;
	public string player_name = "Player";
	public GameObject plobj;
	static int crate = 20;
	static int srate = 20;
	static int maxplayers = 16;
	static int port = 5300;
	static string tport = "5300";
	
	int version = 24;
	int map = -1;
	public bool isenabled = false;
	bool notdisc = false;
	bool ischange = false;
	bool nat = false;
	
	public static AudioClip[] death_snd;
	public static Object deathpr;
	static bool loaded = false;
	
	//Master server
	System.Net.WebClient wc;
	System.Collections.Specialized.NameValueCollection post_valid;
	
	//Banlist
	public static List<string> banlist = new List<string>();
	
	bool InitServData()
	{
		if(masterurl == null)
		{
			masterurl = CFGLoader.srvlist;
			if(masterurl.Length < 1)
				return false;
		}
		if(post_valid == null)
			post_valid = new System.Collections.Specialized.NameValueCollection()
			{
				{"valid","sniper"},
				{"port",port.ToString()},
				{"name",CFGLoader.serv_name}
			};
		return true;
	}
	
	void Awake()
	{
		if(loaded) return;
		CFGLoader.LoadData();
		loaded = true;
	}
	
	void Start()
	{
		map = Application.loadedLevel;
		if(!isloaded)
		{
			if(File.Exists(file_addr))
			{
				StreamReader sr = new StreamReader(file_addr);
				if(sr.Peek() > -1)
					ip = sr.ReadLine();
				if(sr.Peek() > -1)
					tport = sr.ReadLine();
				sr.Close();
			}
			LoadBans();
			isloaded = true;
		}
		if(GameObject.Find("ChangeServ") || GameObject.Find("ChangeClient")) ischange = true;
		if(map == 0)
		{
			if(CFGLoader.dedicated)
				Application.LoadLevel(CFGLoader.lvl);
			masterurl = CFGLoader.srvlist;
			if(masterurl.Length <= 0) masterurl = null;
		}
	}
	
	public Transform GetSpawn()
	{
		return spawns[Random.Range(0,spawns.Length)];
	}
	
	public static void AddBan(string input)
	{
		banlist.Add(input);
		StreamWriter sw = new StreamWriter(file_bans,true);
		sw.WriteLine(input);
		sw.Close();
	}
	
	public static void LoadBans()
	{
		banlist.Clear();
		string tmp;
		StreamReader sr = new StreamReader(file_bans);
		while(sr.Peek() > -1)
		{
			tmp = sr.ReadLine();
			if(tmp.Length > 0)
				banlist.Add(tmp);
		}
		sr.Close();
	}
	
	public void LoadData()
	{
		player_name = CFGLoader.pname;
		crate = CFGLoader.crate;
		srate = CFGLoader.srate;
		maxplayers = CFGLoader.maxplayers;
		port = CFGLoader.port;
		PlayerPrefab = Resources.LoadAll("Player");
		if(CFGLoader.dedicated)
		{
			AudioListener.volume = 0;
			Network.InitializeServer(maxplayers, port, false);
			GameObject.Instantiate(Resources.Load("HUD/hud_items")).name = "hud_items";
			GetComponent<ChatGUI>().LoadChat();
			Transform tmp = GameObject.Find("Spawns").transform;
			spawns = new Transform[tmp.childCount];
			for(int i = 0; i < spawns.Length; i++)
				spawns[i] = tmp.GetChild(i);
			tmp = null;
			plobj = (GameObject)Network.Instantiate(Resources.Load("SrvPlayer"), Vector3.zero, new Quaternion(0,0,0,0), 0);
			plobj.GetComponent<MainScript>().enabled = true;
			GetComponent<NetScore>().StartScanning();
			if(deathpr == null)
			{
				death_snd = new AudioClip[32];
				deathpr = Resources.Load("DeathSound");
			}
			camera.enabled = false;
			connected = true;
		}
	}
	
	public void CreatePlayer()
    {
		Transform tmp = GameObject.Find("Spawns").transform;
		spawns = new Transform[tmp.childCount];
		for(int i = 0; i < spawns.Length; i++)
			spawns[i] = tmp.GetChild(i);
		tmp = null;
		GameObject.Instantiate(Resources.Load("HUD/hud_items")).name = "hud_items";
		GetComponent<ChatGUI>().LoadChat();
		int rnd = Random.Range(0,spawns.Length);
        connected = true;
        plobj = (GameObject)Network.Instantiate(PlayerPrefab[CFGLoader.skin], spawns[rnd].transform.position, spawns[rnd].transform.rotation, 0);
		if(Network.isClient)
			networkView.RPC("ValidateClient",RPCMode.Server,plobj.networkView.viewID,version,map);
		else
			GetComponent<NetScore>().StartScanning();
		camera.enabled = false;
		GetComponent<AudioListener>().enabled = false;
		plobj.GetComponent<MainScript>().SendCustomMessage(RPCMode.Others,player_name + " csatlakozott a szerverre.");
		//Load Sounds
		if(death_snd == null)
		{
			death_snd = System.Array.ConvertAll(Resources.LoadAll("DeathSnd",typeof(AudioClip)),x => (AudioClip)x);
			deathpr = Resources.Load("DeathSound");
		}
	}
	
    void OnDisconnectedFromServer()
    {
		if(!GameObject.Find("ChangeServ") && !GameObject.Find("ChangeClient"))
		{
			if(Network.isServer && CFGLoader.srvvis && nat)
			{
				try
				{
					if(InitServData())
					{
						wc = new System.Net.WebClient();
						wc.UploadValues(masterurl+"/remsvr.php",post_valid);
						wc.Dispose();
					}
				}
				catch{}
			}
			Application.LoadLevel(0);
		}
        connected = false;
    }
	
    void OnPlayerDisconnected(NetworkPlayer pl)
    {
		if(!notdisc)
		{
			Network.RemoveRPCs(pl);
    		Network.DestroyPlayerObjects(pl);
			GetComponent<NetScore>().ServRemID(pl);		
		}
		else
			notdisc = false;
    }
	
    void OnConnectedToServer()
    {
		if(map != 0)
		{
       		CreatePlayer();
			GetComponent<NetScore>().StartScanning();
		}
		else
			networkView.RPC("ValidateClient",RPCMode.Server,Network.AllocateViewID(),version,map);
		Network.sendRate = crate;
    }
	
	void OnFailedToConnect(NetworkConnectionError error)
	{
        WriteErrorLog(error.ToString());
    }
	
    void OnServerInitialized()
    {
		if(!CFGLoader.dedicated)
        	CreatePlayer();
		Network.sendRate = srate;
		//Register server if visible
		if(CFGLoader.srvvis)
			StartCoroutine(RegisterMaster());
    }
	
	int width, height;
	bool showlist = false;
	string[] servlist;
	string[] addrlist;
	int sel_offset = 0;
	string tmpstr;
	string masterurl = null;
	int f;
	
	void OnGUI()
	{
		if(!connected)
		{
			width = Screen.width / 2;
			height = Screen.height / 2;
			if(map == 0 && isenabled)
			{
				ip = GUI.TextField(new Rect(width - 100, height - 48, 180, 24), ip);
				tport = GUI.TextField(new Rect(width + 96, height - 48, 48, 24), tport, 5);
	            if (GUI.Button(new Rect(width - 100, height - 24, 160, 24), "Csatlakozás"))
	            {
					int.TryParse(tport, out port);
					Network.Connect(ip, port);
					StreamWriter sw = new StreamWriter(file_addr,false);
					sw.WriteLine(ip);
					sw.Write(port);
					sw.Close();
	            }
				if(masterurl != null)
				{
					if(GUI.Button(new Rect(width + 64, height - 24, 160, 24), "Keresés"))
					{
						showlist = true;
						if(showlist)
						{
							try
							{
								System.Net.WebClient client = new System.Net.WebClient();
								Stream webstream = client.OpenRead(masterurl+"/list.txt");
								client.Dispose();
								StreamReader sr = new StreamReader(webstream,System.Text.Encoding.Default);
								servlist = sr.ReadToEnd().Split('\n');
								if(servlist[servlist.Length-1].Length < 1)
									System.Array.Resize(ref servlist,servlist.Length-1);
								addrlist = new string[servlist.Length];
								for(f = 0; f < servlist.Length; f++)
								{
									addrlist[f] = servlist[f].Substring(0,servlist[f].IndexOf(';'));
									servlist[f] = servlist[f].Substring(servlist[f].IndexOf(';')+1);
								}
								sr.Close();
								webstream.Close();
							}
							catch
							{
								WriteErrorLog("Sikertelen lekérdezés");
								showlist = false;
							}
						}
					}
				}
				if(showlist)
				{
					for(f = 0; f < 4; f++)
					{
						if(servlist.Length == f) break;
						if(GUI.Button(new Rect(width -100, height + 32 + (f*32), 384, 24),servlist[f + sel_offset]))
						{
							tmpstr = addrlist[f + sel_offset];
							ip = tmpstr.Substring(0,tmpstr.IndexOf(':'));
							tport = tmpstr.Substring(tmpstr.IndexOf(':')+1);
						}
					}
					if(servlist.Length > 4)
					{
						if(GUI.Button(new Rect(width -100, height, 64, 24),"Fel"))
						{
							if(sel_offset > 0)
								sel_offset-=1;
						}
						if(GUI.Button(new Rect(width -100, height + 160, 64, 24),"Le"))
						{
							if(sel_offset < servlist.Length-4)
								sel_offset+=1;
						}
					}
				}
				if(Event.current.Equals(Event.KeyboardEvent("escape")))
				{
					GameObject.Find("menu").SetActiveRecursively(true);
					GameObject.Find("ConnectMenu").SetActiveRecursively(false);
					isenabled = false;
				}
			}
			else if(map != 0)
			{
				if(ischange)
				{
					if(GameObject.Find("ChangeServ"))
					{
						Network.InitializeServer(maxplayers-1, port, false);
						Destroy(GameObject.Find("ChangeServ"));
						ischange = false;
					}
					else if(GameObject.Find("ChangeClient"))
					{
						if(!isloaded)
						{
							StreamReader sr = new StreamReader(file_addr);
							ip = sr.ReadLine();
							if(sr.Peek() > -1)
								tport = sr.ReadLine();
							sr.Close();
							isloaded = true;
						}
						int.TryParse(tport, out port);
						Network.Connect(ip, port);
						Destroy(GameObject.Find("ChangeClient"));
						ischange = false;
					}
				}
				else
				{
					if (GUI.Button(new Rect(width - 100, height - 16, 200, 24), "Szerver indítása"))
		                Network.InitializeServer(maxplayers-1, port, false);
					if (GUI.Button(new Rect(width - 100, height + 16, 200, 24), "Vissza a menübe"))
		            {
						Network.Disconnect();
						Application.LoadLevel(0);
		        		connected = false;
					}
				}
			}
		}
	}
	
	[RPC]
	void ValidateClient(NetworkViewID id, int ver, int map_numb)
	{
		if(banlist.Contains(id.owner.ipAddress))
			CallError(id.owner,"Ki vagy tiltva a szerverről!");
		if(ver != version)
			CallError(id.owner,"Rossz verzió! Szerver: " + version + " | Kliens: " + ver);
		else if(map != map_numb || map_numb == 0)
		{
			networkView.RPC("CorrectMap",id.owner,map);
			Network.CloseConnection(id.owner,true);
			notdisc = true;
		}
		else
			networkView.RPC("ValidateClient2",id.owner);
	}
	
	[RPC]
	void ValidateClient2()
	{
		GetComponent<NetScore>().ServAddPlayer(player_name,plobj.networkView.viewID);
	}
	
	public void ChangeLevel(int lvl)
	{	
		if(Network.isServer)
			DontDestroyOnLoad(new GameObject("ChangeServ"));
		else
			DontDestroyOnLoad(new GameObject("ChangeClient"));
		Application.LoadLevel(lvl);
		networkView.RPC("DoChangeLevel",RPCMode.Others,lvl);
	}
	
	[RPC]
	void DoChangeLevel(int map_numb)
	{
		if(Network.isServer)
			DontDestroyOnLoad(new GameObject("ChangeServ"));
		else
			DontDestroyOnLoad(new GameObject("ChangeClient"));
		Application.LoadLevel(map_numb);
	}
	
	[RPC]
	void CorrectMap(int map_numb)
	{
		Network.Disconnect();
		Application.LoadLevel(map_numb);
        connected = false;
		int.TryParse(tport, out port);
		Network.Connect(ip, port);
		connected = true;
	}

	public void CallError(NetworkPlayer id, string data)
	{
		networkView.RPC("WriteErrorLog",id,data);
		Network.CloseConnection(id, true);
	}
	
	[RPC]
	public void WriteErrorLog(string data)
	{
		connected = false;
		if(GameObject.Find("Error"))
			Destroy(GameObject.Find("Error"));		
		GameObject error = new GameObject("Error");
		error.AddComponent("ErrorMessage");
		error.GetComponent<ErrorMessage>().ShowError("HIBA: " + data);
		DontDestroyOnLoad(error);
	}
	
	IEnumerator RegisterMaster()
	{
		ConnectionTesterStatus con_test;
		while((con_test = Network.TestConnectionNAT()) == ConnectionTesterStatus.Undetermined)
			yield return new WaitForSeconds(5.0f);
		try
		{
			if(nat = (con_test == ConnectionTesterStatus.PublicIPIsConnectable))
			{
				if(InitServData())
				{
					wc = new System.Net.WebClient();
					wc.UploadValues(masterurl+"/regsvr.php",post_valid);
					wc.Dispose();
					StartCoroutine(UpdateMaster());
				}
			}
		}
		catch{}
	}
	
	IEnumerator UpdateMaster()
	{
		while(true)
		{
			yield return new WaitForSeconds(3540f);
			try
			{
				InitServData();
				wc = new System.Net.WebClient();
				wc.UploadValues(masterurl+"/updsvr.php",post_valid);
				wc.Dispose();
			}
			catch{}
		}
	}
}