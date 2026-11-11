namespace PRI.Domain.AiGateway;

/// <summary>Embedding vektörleri için saf matematik. Vektörler birim uzunluğa normalize edilirse kosinüs benzerliği iç çarpıma eşittir.</summary>
public static class EmbeddingMath
{
    public static float[] Normalize(IReadOnlyList<float> vector)
    {
        double sumSquares = 0;
        for (var i = 0; i < vector.Count; i++)
        {
            sumSquares += (double)vector[i] * vector[i];
        }

        var result = new float[vector.Count];
        var norm = Math.Sqrt(sumSquares);
        if (norm == 0)
        {
            return result;
        }

        for (var i = 0; i < vector.Count; i++)
        {
            result[i] = (float)(vector[i] / norm);
        }

        return result;
    }

    /// <summary>Normalize edilmiş iki vektörün kosinüs benzerliği (-1..1). Boyutlar farklıysa -1 döner (karşılaştırılamaz).</summary>
    public static double Similarity(float[] a, float[] b)
    {
        if (a.Length == 0 || a.Length != b.Length)
        {
            return -1d;
        }

        double dot = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += (double)a[i] * b[i];
        }

        return dot;
    }
}
