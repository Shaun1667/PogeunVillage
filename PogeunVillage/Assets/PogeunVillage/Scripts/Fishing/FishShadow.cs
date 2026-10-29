using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 강의 물고기 그림자 하나(3-6a): 생긴 자리 둘레를 천천히 떠다님. 찌가 가까이 떨어지면 찌로 다가감(FishingRodUser).
    /// 어떤 물고기인지는 낚기 전까지 크기로만 짐작.
    /// </summary>
    public class FishShadow : MonoBehaviour
    {
        public FishDefinition Fish { get; private set; }
        public Vector2Int Cell { get; private set; }
        public bool IsAttracted { get; private set; }

        Vector3 home, target;
        float wanderRadius, wanderSpeed;
        IRandomSource random;
        Vector3 attractFrom, attractTo;
        float attractStart, attractSeconds;

        public void Setup(FishDefinition fish, Vector2Int cell, float radius, float speed, IRandomSource wanderRandom)
        {
            Fish = fish;
            Cell = cell;
            home = target = transform.position;
            wanderRadius = radius;
            wanderSpeed = speed;
            random = wanderRandom;
        }

        /// <summary>찌로 다가감(seconds 동안)</summary>
        public void Attract(Vector3 to, float seconds)
        {
            IsAttracted = true;
            attractFrom = transform.position;
            attractTo = new Vector3(to.x, transform.position.y, to.z);
            attractStart = Time.time;
            attractSeconds = Mathf.Max(0.01f, seconds);
        }

        void Update()
        {
            if (IsAttracted)
            {
                float k = Mathf.Clamp01((Time.time - attractStart) / attractSeconds);
                transform.position = Vector3.Lerp(attractFrom, attractTo, k * k * (3f - 2f * k));
                return;
            }
            if (random == null)
                return;
            if ((transform.position - target).sqrMagnitude < 0.01f)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float r = (float)random.NextDouble() * wanderRadius;
                target = home + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
            }
            Vector3 step = Vector3.MoveTowards(transform.position, target, wanderSpeed * Time.deltaTime);
            if (step != transform.position)
                transform.rotation = Quaternion.LookRotation(step - transform.position);
            transform.position = step;
        }
    }
}
