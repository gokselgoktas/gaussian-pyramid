using UnityEngine;

[RequireComponent(typeof (GaussianPyramid))]
public class Viewer : MonoBehaviour
{
    [Range(0f, 16f)]
    public float lod = 0;

    private Shader m_Shader;
    public Shader shader
    {
        get
        {
            if (m_Shader == null)
                m_Shader = Shader.Find("Hidden/Viewer");

            return m_Shader;
        }
    }

    private Material m_Material;
    public Material material
    {
        get
        {
            if (m_Material == null)
            {
                if (shader == null || shader.isSupported == false)
                    return null;

                m_Material = new Material(shader);
            }

            return m_Material;
        }
    }

    private RenderTexture m_Pyramid
    {
        get
        {
            var pyramid = GetComponent<GaussianPyramid>();

            if (pyramid == null)
                return null;

            return pyramid.texture;
        }
    }

    public void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (m_Pyramid == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        material.SetFloat("_LOD", lod);
        Graphics.Blit(m_Pyramid, destination, material);
    }
}
