using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IInitializable = Zenject.IInitializable;

namespace Controllers
{
    public class AudioController : MonoBehaviour, IInitializable
    {
        public const string SelectKey = "select";
        public const string MatchKey = "match";
        public const string ShuffleKey = "shuffle";
        
        private AudioSource _source;
        private List<AudioClip> _clips = new List<AudioClip>();

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
        }

        private void Start()
        {
            _clips = Resources.LoadAll<AudioClip>("Audio/Sounds/").ToList();
        }

        public void PlayOne(string key)
        {
            var clip = Resources.Load<AudioClip>($"Audio/Sounds/{key}");
            _source.PlayOneShot(_clips.FirstOrDefault( c => c.name == key));
        }

        public void Initialize()
        {
            
        }
    }
}