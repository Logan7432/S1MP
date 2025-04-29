using System;
using System.Collections;
using UnityEngine;

namespace SteamGameServerMod.Client
{
    public class LeanTween
    {
        public static LTDescr value(GameObject go, float from, float to, float duration)
        {
            LTDescr descr = new LTDescr(go, from, to, duration);
            return descr;
        }
    }

    public class LTDescr
    {
        private GameObject _gameObject;
        private float _from;
        private float _to;
        private float _duration;
        private Action<float> _onUpdate;
        private Action _onComplete;

        public LTDescr(GameObject go, float from, float to, float duration)
        {
            _gameObject = go;
            _from = from;
            _to = to;
            _duration = duration;

            // Start animation automatically
            UnityMainThreadDispatcher.Instance().EnqueueCoroutine(AnimateValue());
        }

        public LTDescr setOnUpdate(Action<float> onUpdate)
        {
            _onUpdate = onUpdate;
            return this;
        }

        public LTDescr setOnComplete(Action onComplete)
        {
            _onComplete = onComplete;
            return this;
        }

        private IEnumerator AnimateValue()
        {
            float startTime = Time.time;
            float endTime = startTime + _duration;
            float currentValue = _from;

            while (Time.time < endTime)
            {
                float t = (Time.time - startTime) / _duration;
                currentValue = Mathf.Lerp(_from, _to, t);

                _onUpdate?.Invoke(currentValue);

                yield return null;
            }

            _onUpdate?.Invoke(_to);
            _onComplete?.Invoke();
        }
    }
}