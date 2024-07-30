using UnityEngine;

using DG.Tweening;
using SlotGame;
using SlotGame.Sound;

namespace SlotGame.Machine.S1018
{
    public class WildParticle1018 : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField] private CurveMovement wildParticleReference;
        [SerializeField] private GameObject wildParticlePool;
        [SerializeField] private GameObject wildParticleRoot;

        [SerializeField] private string blueWildSymbolID;
        [SerializeField] private string redWildSymbolID;

        [Space(10)]
        [SerializeField] private float particleSequenceTime;
        [SerializeField] private float particleLiftTime;

        [Header("Color")]
        [SerializeField] private Color blueColor;
        [SerializeField] private Color redColor;

        [Header("Final Color")]
        [SerializeField] private Color finalBlueColor;
        [SerializeField] private Color finalRedColor;

        [Header("Sound")]
        [SerializeField] private SoundPlayer blue_particle_sound;
        [SerializeField] private SoundPlayer red_particle_sound;

#pragma warning restore 0649

        private GameObjectPool<CurveMovement> wildParticleOP;
        private ParticleSystem childParticle;
        private SoundPlayer particleSound;

        private void Awake()
        {
            wildParticleOP = new GameObjectPool<CurveMovement>(wildParticlePool,
                                                            5,
                                                            () =>
                                                            {
                                                                return Instantiate(wildParticleReference);
                                                            });
        }

        public void PlayParticleSequnece(Symbol symbol, Vector3 targetPos, out float delayTime)
        {
            delayTime = particleLiftTime;
            // 파티클 On
            var particle = wildParticleOP.Get();
            particle.CachedTransfom.SetParent(wildParticleRoot.transform, false);

            var particlePos = symbol.transform.position;
            particlePos.z = wildParticleRoot.transform.position.z;
            particle.CachedTransfom.position = particlePos;
            particle.CachedTransfom.localScale = Vector3.one;

            var particleSystem = particle.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule mainModule = particleSystem.main;

            childParticle = particle.CachedTransfom.GetChild(0).GetComponent<ParticleSystem>();
            ParticleSystem.MainModule childModule = childParticle.main;

            if (symbol.id == blueWildSymbolID)
            {
                targetPos = new Vector3(targetPos.x, targetPos.y, particlePos.z);
                mainModule.startColor = new ParticleSystem.MinMaxGradient(blueColor);
                childModule.startColor = new ParticleSystem.MinMaxGradient(finalBlueColor);
                particleSound = blue_particle_sound;
            }
            else if (symbol.id == redWildSymbolID)
            {
                targetPos = new Vector3(targetPos.x, targetPos.y, particlePos.z);
                mainModule.startColor = new ParticleSystem.MinMaxGradient(redColor);
                childModule.startColor = new ParticleSystem.MinMaxGradient(finalRedColor);
                particleSound = red_particle_sound;
            }
            else
            {
                Debug.Log("Symbol ID is Not Wild");
                return;
            }

            //------------------------------------------------------------------------------
            // 시퀀스
            //------------------------------------------------------------------------------
            var seq = DOTween.Sequence();
            seq.AppendCallback(() =>
            {
                particle.Move(targetPos);
                particleSound.Play();
            });
            seq.AppendInterval(particleSequenceTime);
            seq.AppendCallback(() =>
            {
                wildParticleOP.Return(particle);
                particle.transform.position = Vector3.zero;
                particle.CachedTransfom.localScale = Vector3.one;
            });
            //------------------------------------------------------------------------------
        }
    }
}