using UnityEngine;

namespace CheesyFX
{
    public static class NormalDistribution
    {
		public static float GetValue(float mean = 0.0f, float delta = 1.0f, float sharpness = 3f, bool onesided = false, bool useNormalD = true)
		{
			if (delta == 0f) return mean;
			if(!useNormalD)
			{
				return onesided ? Random.Range(mean, mean+delta) : Random.Range(mean-delta, mean+delta);
			}
			float u, v, S;
			if (delta == 0f)
			{
				return mean;
			}
			do
			{
				u = 2.0f * UnityEngine.Random.value - 1.0f;
				v = 2.0f * UnityEngine.Random.value - 1.0f;
				S = u * u + v * v;
			}
			while (S >= 1.0f);

			// Standard Normal Distribution
			float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);

			// Normal Distribution centered between the min and max value
			// and clamped following the "three-sigma rule"
			//float mean = (minValue + maxValue) / 2.0f;
			float deltaAbs = Mathf.Abs(delta);
			float sigma = deltaAbs / sharpness;
			float output = Mathf.Clamp(std * sigma + mean, mean - deltaAbs, mean + deltaAbs);
			if(onesided) output = Mathf.Sign(delta)*Mathf.Abs(output-mean) + mean;
			return output;
		}
	}
}