using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BotScript : MonoBehaviour {
	
	public GameObject skin;
	public Transform proj_point;
	
	public Renderer[] weapons;
	public AudioClip[] wsound;
	int wid = 0;
	int hp = 100;
	
	Transform[] players;
	Transform target;
	NavMeshAgent nav;
	
	RaycastHit hit = new RaycastHit();
	NetworkMenu gamecontrol;
	GUIText resmsg;
	
	//Death Snd
	int death_len;
	GameObject tmpdeath;
	
	void Awake()
	{
		skin.animation["fire"].layer = 1;
		name = "Bot";
		gamecontrol = GameObject.Find("GameControl").GetComponent<NetworkMenu>();
		if(Network.isServer)
		{
			resmsg = GameObject.Find("HudRespawn").guiText;
			death_len = NetworkMenu.death_snd.Length;
			nav = GetComponent<NavMeshAgent>();
			SpawnBot();
			wid = Random.Range(0,weapons.Length);
			weapons[wid].renderer.enabled = true;
			StartCoroutine(ChangeTarget());
			StartCoroutine(ShootTimer());
			StartCoroutine(IdleTimer());
		}
		else
			networkView.RPC("RequestWid",RPCMode.Server);
	}
	
	[RPC]
	void RequestWid(NetworkMessageInfo info)
	{
		networkView.RPC("SendWid", info.sender, wid, skin.animation.clip.name);
	}
	
	[RPC]
	void SendWid(int weapid, string anim)
	{
		wid = weapid;
		weapons[wid].renderer.enabled = true;
		skin.animation.clip = skin.animation.GetClip(anim);
		skin.animation.Play();
	}
	
	void SpawnBot()
	{
		transform.position = gamecontrol.GetSpawn().position;
		nav.speed = 7;
		DoAnim("run");
		if(!enabled)
		{
			enabled = true;
			nav.enabled = true;
		}
	}
	
	void FixedUpdate()
	{
		if(target == null)
			target = gamecontrol.plobj.transform;
		nav.destination = target.position;
		proj_point.LookAt(target.position);
		
		if(Vector3.Distance(transform.position,target.position) < 5)
		{
			if(skin.animation.clip.name == "run")
			{
				nav.speed = 0;
				DoAnim("idle");
			}
			transform.LookAt(new Vector3(target.position.x,transform.position.y,target.position.z));
		}
		else
		{
			if(skin.animation.clip.name == "idle")
			{
				nav.speed = 7;
				DoAnim("run");
			}
		}
	}
	
	IEnumerator ChangeTarget()
	{
		while(true)
		{
			yield return new WaitForSeconds(15);
			players = System.Array.ConvertAll(GameObject.FindGameObjectsWithTag("Player"), x => x.transform);
			target = players[Random.Range(0,players.Length)];
			players = null;
		}
	}
	
	IEnumerator ShootTimer()
	{
		while(true)
		{
			yield return new WaitForSeconds(1);
			if(enabled)
			{
				if(Physics.Raycast(proj_point.position,proj_point.forward,out hit,50.0f))
				{
					if(hit.collider.name.Contains("Player"))
					{
						if(hit.collider.networkView.isMine && !resmsg.enabled)
						{
							DoAnim("fire");
							networkView.RPC("SendShot",RPCMode.All,wid);
							if(Random.Range(0,100) < 40)
							{
								hit.collider.GetComponent<MainScript>().SendDamage(null,Random.Range(5,25),true);
								hit.collider.GetComponent<MainScript>().RequestObject(0, hit.point);
							}
						}
						else if(!hit.collider.networkView.isMine)
						{
							DoAnim("fire");
							networkView.RPC("SendShot",RPCMode.All,wid);
							if(Random.Range(0,100) < 40)
							{
								networkView.RPC("GetDamage",hit.collider.networkView.owner);
								hit.collider.GetComponent<MainScript>().RequestObject(0, hit.point);
							}
						}
					}
				}
			}
		}
	}
	
	IEnumerator IdleTimer()
	{
		while(true)
		{
			yield return new WaitForSeconds(Random.Range(20,30));
			if(enabled)
			{
				nav.speed = 0;
				DoAnim("idle");
				StartCoroutine(RestoreTimer(5));
			}
		}
	}
				
	IEnumerator RestoreTimer(float sec)
	{
		yield return new WaitForSeconds(sec);
		if(enabled)
		{
			DoAnim("run");
			nav.speed = 7;
		}
		if(skin.light.enabled)
			skin.light.enabled = false;
	}
	
	IEnumerator ResetFlash()
	{
		yield return new WaitForSeconds(0.05f);
		weapons[wid].transform.GetChild(0).renderer.enabled = false;
	}
	
	void DoAnim(string anim)
	{
		networkView.RPC("ChangeAnim",RPCMode.All,anim);
	}
	
	[RPC]
	void ChangeAnim(string anim)
	{
		if(anim == "fire")
		{
			skin.animation.Stop(anim);
			skin.animation.Play(anim);
		}
		else
		{
			skin.animation.clip = skin.animation.GetClip(anim);
			skin.animation.CrossFade(anim);
		}
	}
	
	[RPC]
	void SendShot(int id)
	{
		audio.PlayOneShot(wsound[id]);
		weapons[wid].transform.GetChild(0).renderer.enabled = true;
		skin.light.enabled = true;
		StartCoroutine(RestoreTimer(0.2f));
		StartCoroutine(ResetFlash());
	}
	
	[RPC]
	void GetDamage()
	{
		gamecontrol.plobj.GetComponent<MainScript>().SendDamage(null,Random.Range(5,25),true);
	}
	
	public void SendSetTarget(Transform player, int damage)
	{
		if(Network.isClient)
			networkView.RPC("SetTarget",RPCMode.Server,player.networkView.viewID, damage);
		else
			SetTarget(player.networkView.viewID, damage);
	}
	
	[RPC]
	void SetTarget(NetworkViewID player, int damage)
	{
		target = NetworkView.Find(player).transform;
		hp -= damage;
		if(hp <= 0)
		{
			DoDeath();
			networkView.RPC("PlayDeathSnd",RPCMode.All,Random.Range(0,death_len));
			if(player.isMine)
				AddScore();
			else
				networkView.RPC("AddScore",player.owner);
			target.gameObject.GetComponent<MainScript>().SendBotMessage(player);
		}
	}
	
	[RPC]
	void AddScore()
	{
		if(gamecontrol == null)
			gamecontrol = GameObject.Find("GameControl").GetComponent<NetworkMenu>();
		gamecontrol.GetComponent<NetScore>().ServAddScore(gamecontrol.player_name,1);
		gamecontrol.plobj.GetComponent<MainScript>().SendCustomMessage(RPCMode.All, gamecontrol.player_name + " megölt egy botot");
	}
	
	void DoDeath()
	{
		enabled = false;
		nav.enabled = false;
		hp = 100;
		nav.speed = 0;
		DoAnim("death");
		StartCoroutine(RespawnTimer());
	}
	
	IEnumerator RespawnTimer()
	{
		yield return new WaitForSeconds(2);
		SpawnBot();
		networkView.RPC("ChangeState",RPCMode.All);
	}
	
	[RPC]
	void ChangeState()
	{
		collider.enabled = true;
	}
	
	[RPC]
	void PlayDeathSnd(int numb)
	{
		collider.enabled = false;
		tmpdeath = (GameObject)GameObject.Instantiate(NetworkMenu.deathpr,
			transform.position, transform.rotation);
		tmpdeath.audio.PlayOneShot(NetworkMenu.death_snd[numb]);
		tmpdeath = null;
	}
}