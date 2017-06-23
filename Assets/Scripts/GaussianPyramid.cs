using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof (Camera))]
public class GaussianPyramid : MonoBehaviour
{
    private const int MAXIMUM_BUFFER_SIZE = 2048;

    private Shader m_Shader;
    public Shader shader
    {
        get
        {
            if (m_Shader == null)
                m_Shader = Shader.Find("Hidden/Gaussian Pyramid");

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

    private Camera m_Camera;
    public new Camera camera
    {
        get
        {
            if (m_Camera == null)
                m_Camera = GetComponent<Camera>();

            return m_Camera;
        }
    }

    private RenderTexture m_Pyramid;
    public RenderTexture texture
    {
        get
        {
            return m_Pyramid;
        }
    }

    private int m_LODCount = 0;
    public int lodCount
    {
        get
        {
            if (m_Pyramid == null)
                return 0;

            return 1 + m_LODCount;
        }
    }

    private CommandBuffer m_CommandBuffer;
    private CameraEvent m_CameraEvent = CameraEvent.AfterImageEffectsOpaque;

    private int[] m_Temporaries;

    void OnEnable()
    {
        camera.depthTextureMode = DepthTextureMode.Depth;
    }

    void OnDisable()
    {
        if (camera != null)
        {
            if (m_CommandBuffer != null)
            {
                camera.RemoveCommandBuffer(m_CameraEvent, m_CommandBuffer);
                m_CommandBuffer = null;
            }
        }

        if (m_Pyramid != null)
        {
            m_Pyramid.Release();
            m_Pyramid = null;
        }
    }

    void OnPreRender()
    {
        int size = (int) Mathf.Max((float) camera.pixelWidth, (float) camera.pixelHeight);
        size = (int) Mathf.Min((float) Mathf.NextPowerOfTwo(size), (float) MAXIMUM_BUFFER_SIZE);

        m_LODCount = (int) Mathf.Floor(Mathf.Log(size, 2f));

        if (m_LODCount == 0)
            return;

        if (m_Pyramid == null || (m_Pyramid.width != size || m_Pyramid.height != size))
        {
            if (m_Pyramid != null)
                m_Pyramid.Release();

            m_Pyramid = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            m_Pyramid.filterMode = FilterMode.Trilinear;

            m_Pyramid.useMipMap = true;
            m_Pyramid.autoGenerateMips = false;

            m_Pyramid.Create();

            m_Pyramid.hideFlags = HideFlags.HideAndDontSave;

            if (m_CommandBuffer != null)
            {
                camera.RemoveCommandBuffer(m_CameraEvent, m_CommandBuffer);
                m_CommandBuffer = null;
            }
        }

        if (m_CommandBuffer == null)
        {
            m_Temporaries = new int[m_LODCount << 1];

            if (m_CommandBuffer != null)
                camera.RemoveCommandBuffer(m_CameraEvent, m_CommandBuffer);

            m_CommandBuffer = new CommandBuffer();
            m_CommandBuffer.name = "Gaussian Pyramid";

            RenderTargetIdentifier id = new RenderTargetIdentifier(m_Pyramid);

            m_CommandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, id);

            for (int i = 0; i < (m_LODCount << 1); i += 2)
            {
                int lod = i >> 1;

                m_Temporaries[i] = Shader.PropertyToID("_MainTex");
                m_Temporaries[i + 1] = Shader.PropertyToID("_162002c1_Temporaries" + (i + 1).ToString());

                size >>= 1;

                if (size == 0)
                    size = 1;

                m_CommandBuffer.GetTemporaryRT(m_Temporaries[i], size, size, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                m_CommandBuffer.GetTemporaryRT(m_Temporaries[i + 1], size, size, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

                m_CommandBuffer.SetGlobalVector("_Spread", Vector2.one);
                m_CommandBuffer.SetGlobalVector("_TexelSize", Vector2.one / (float) (size - 1));

                m_CommandBuffer.SetGlobalFloat("_LOD", (float) lod);

                m_CommandBuffer.SetGlobalVector("_Direction", Vector2.right);
                m_CommandBuffer.Blit(id, m_Temporaries[i], material);

                m_CommandBuffer.SetGlobalVector("_Direction", Vector2.down);
                m_CommandBuffer.Blit(m_Temporaries[i], m_Temporaries[i + 1], material);

                m_CommandBuffer.CopyTexture(m_Temporaries[i + 1], 0, 0, id, 0, lod + 1);

                m_CommandBuffer.ReleaseTemporaryRT(m_Temporaries[i]);
                m_CommandBuffer.ReleaseTemporaryRT(m_Temporaries[i + 1]);
            }

            camera.AddCommandBuffer(m_CameraEvent, m_CommandBuffer);
        }
    }
}
