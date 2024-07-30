using System.Collections;
using UnityEngine;
using DG.Tweening;

using SlotGame.UI;

namespace SlotGame.Machine.S1031
{
    public class LightningLine1031 : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField] private float interval;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private ParticleSystem dotStartParticle;
        [SerializeField] private ParticleSystem dotEndParticle;
        [SerializeField] private float defaultProgressDuration;
        [SerializeField] private float progressDurationMin;
        [SerializeField] private float elapseTimeInterval;
        [SerializeField] private float factorDuration;
        [SerializeField] private Ease progressEase;
        [SerializeField] private Ease factorEase;
        [SerializeField] private ResolutionTransform resolutionTr;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private float endPointInterval;

#pragma warning restore 0649

        private float progressDuration = 0.0f;
        private readonly int ROW_COUNT = 5;
        private readonly int positionCount = 2;
        private int showCount = 0;
        private Vector3 lineEndPos = Vector3.zero;
        private float totalDuration = 0.0f;
        private float resolutionRate = 0.0f;


        private readonly string shaderProgress = "_Progress";
        private readonly string shaderFactor = "_Factor";

        private void Awake()
        {
            HideLightning();

            progressDuration = defaultProgressDuration;
            showCount = 0;
        }

        public void StartLightning(int linkReelIndex, Vector3 startPos, out float duration)
        {
            if (this.gameObject.activeSelf == false)
            {
                this.gameObject.SetActive(true);
            }

            resolutionRate = (1.0f - resolutionTr.transform.localScale.x) * 10;
            float endPointResolution = resolutionRate * endPointInterval;

            lineEndPos.y = endPointResolution;

            progressDuration = progressDuration - (showCount * elapseTimeInterval);
            progressDuration = Mathf.Clamp(progressDuration, progressDurationMin, defaultProgressDuration);

            duration = progressDuration;
            totalDuration = progressDuration + factorDuration;

            StartCoroutine(ShowLightning(linkReelIndex, startPos));
        }

        public IEnumerator ShowLightning(int linkReelIndex, Vector3 startPos)
        {
            int reelIndex = linkReelIndex % ROW_COUNT;
            int mainSymbolIndex = Mathf.Abs((linkReelIndex / ROW_COUNT) - 2);

            // Dot Object Position
            dotStartParticle.transform.position = startPos;
            dotStartParticle.Play();

            dotEndParticle.Play();

            // Lightning Line
            float linePosX = (reelIndex - 2) * interval;
            float linePosY = (mainSymbolIndex + 1) * interval;

            Vector3 startPosition = new Vector3(linePosX, linePosY, 1);

            lineRenderer.positionCount = positionCount;
            lineRenderer.SetPosition(0, lineEndPos);
            lineRenderer.SetPosition(1, startPosition);

            Material[] material = lineRenderer.materials;

            yield return new WaitForSeconds(progressDuration);

            for (int i = 0; i < material.Length; i++)
            {
                material[i].DOFloat(1.0f, shaderFactor, factorDuration).SetEase(factorEase);
            }

            showCount += 1;

            yield return new WaitForSeconds(totalDuration);
        }

        public void InitializeProgressMaterials()
        {
            Material[] material = lineRenderer.materials;
            for (int i = 0; i < material.Length; i++)
            {
                material[i].SetFloat(shaderProgress, 1.0f);
            }
            for (int i = 0; i < material.Length; i++)
            {
                material[i].SetFloat(shaderFactor, 0.0f);
            }

            dotEndParticle.Stop();
        }

        public void HideLightning()
        {
            this.gameObject.SetActive(false);
            InitializeProgressMaterials();
            lineRenderer.positionCount = 0;

            progressDuration = defaultProgressDuration;
            showCount = 0;
        }


        public void LightningStart_Test(int pos)
        {
            if (gameObject.activeSelf == false)
                gameObject.SetActive(true);

            StartCoroutine(LightningStartTestCoroutine(pos));
        }

        private IEnumerator LightningStartTestCoroutine(int pos)
        {
            SlotMachine slotMachine = this.GetComponentInParent<SlotMachine1031>();
            int reelIndex = pos % ROW_COUNT;
            int mainIndex = Mathf.Abs((pos / ROW_COUNT) - 2);

            var symbol = slotMachine.ReelGroup.GetReel(reelIndex).GetMainSymbol(mainIndex);

            float duration = 0.0f;
            StartLightning(pos, symbol.transform.position, out duration);

            yield return new WaitForSeconds(duration + 0.5f);

            InitializeProgressMaterials();

            HideLightning();
        }
    }
}