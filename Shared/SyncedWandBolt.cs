using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ErenshorCoop.Shared
{
	public class SyncedWandBolt : MonoBehaviour
	{
		private void Start()
		{
			
		}

		private void Update()
		{
			if(MyAud != null)
				MyAud.volume = GameData.SFXVol * MyAud.volume;
			if (TargetChar == null || SourceChar == null)
			{
				Destroy(gameObject);
				return;
			}
			if (moveDel <= 0f)
			{
				Vector3 normalized = (TargetChar.transform.position + Vector3.up - transform.position).normalized;
				if (Vector3.Distance(transform.position, TargetChar.transform.position + Vector3.up) > 2f)
				{
					transform.position += normalized * MoveSpeed * Time.deltaTime;
				}
				else
				{
					DeliverDamage();
				}
				MoveSpeed += 10f * Time.deltaTime;
				return;
			}
			moveDel -= 60f * Time.deltaTime;
			if (moveDel < 15f && !didSFX)
			{
				if (AtkSound != null && SourceChar != null)
				{
					SourceChar.MyAudio.PlayOneShot(AtkSound, SourceChar.MyAudio.volume * GameData.SFXVol);
				}
				didSFX = true;
			}
			if (moveDel <= 0f)
			{
				transform.position = SourceChar.transform.position + transform.forward + Vector3.up;
				if (Vector3.Distance(transform.position, TargetChar.transform.position + Vector3.up) > 5f)
				{
					if (MyAud != null)
						MyAud.Play();
				}
			}
		}
		private void DeliverDamage()
		{
			Destroy(gameObject);
		}

		public void LoadWandBolt(int _dmg, Character _tar, Character _caster, float _speed, GameData.DamageType _dmgType, Color _boltCol, AudioClip _atkSound)
		{
			AtkSound = _atkSound;
			TargetChar = _tar;
			SourceChar = _caster;
			MoveSpeed = _speed;
			DmgType = _dmgType;
			var main = MyParticle.main;
			main.startColor = _boltCol;
		}

		public void LoadArrow(int _dmg, Spell _proc, Character _tar, Character _caster, float _speed, GameData.DamageType _dmgType, AudioClip _atkSound, bool _forceEffectOnTarget=false, bool _interrupt=false, int dmgMod = 1)
		{
			forceEffectOntoTarget = _forceEffectOnTarget;
			interrupt = _interrupt;
			this.dmgMod = dmgMod;
			AtkSound = _atkSound;
			Dmg = _dmg;
			if (_proc != null)
			{
				Proc = _proc;
			}
			TargetChar = _tar;
			SourceChar = _caster;
			MoveSpeed = _speed;
			DmgType = _dmgType;
		}



		public Character SourceChar;
		public Character TargetChar;
		public float MoveSpeed;
		public GameData.DamageType DmgType = GameData.DamageType.Magic;
		public ParticleSystem MyParticle;
		public AudioSource MyAud;
		private AudioClip AtkSound;
		private float moveDel = 40f;
		private bool didSFX;
		private bool forceEffectOntoTarget;
		private bool interrupt;
		private int dmgMod = 1;
		private int Dmg;
		private Spell Proc;
	}
}
