using UnityEditor;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

    /**
     * ObjectReferenceBase is a base template container for UnityEngine.Object.
     */ 
    [System.Serializable]
    public class ObjectReferenceBase<T> where T: UnityEngine.Object {

        [SerializeField] private string m_guid;
        private T m_object;

        public T Object {
            get {
                if (m_object == null) {
                    if (!string.IsNullOrEmpty (m_guid)) {
                        var assetPath = AssetDatabase.GUIDToAssetPath (m_guid);
                        if (!string.IsNullOrEmpty (assetPath)) {
                            m_object = AssetDatabase.LoadAssetAtPath<T> (assetPath);
                        }
                    }
                }
                return m_object;
            }
            set {
                if (value == null) {
                    m_object = null;
                    m_guid = null;
                } else {
                    m_guid = AssetDatabase.AssetPathToGUID (AssetDatabase.GetAssetPath(value));
                    m_object = value;
                }
            }
        }

        public string AssetPath
        {
            get
            {
                var path = string.Empty;
                if (!string.IsNullOrEmpty (m_guid)) {
                    path = AssetDatabase.GUIDToAssetPath (m_guid);
                }

                return path;
            }
        }

        public bool Empty {
            get {
                return m_guid == null;
            }
        }

        public ObjectReferenceBase() {
            m_object = null;
            m_guid = null;
        }

        public ObjectReferenceBase(ObjectReferenceBase<T> o) {
            if (o == null) {
                m_object = null;
                m_guid = null;
            } else {
                m_guid = o.m_guid;
                m_object = o.m_object;
            }
        }

        public ObjectReferenceBase(T o) {
            if (o == null) {
                m_object = null;
                m_guid = null;
            } else {
                m_guid = AssetDatabase.AssetPathToGUID (AssetDatabase.GetAssetPath(o));
                m_object = o;
            }
        }

        public override bool Equals(object rhs)
        {
            ObjectReferenceBase<T> o = rhs as ObjectReferenceBase<T>;
            if (o == null) {
                return false;
            }
            
            return o.m_object == m_object;
        }

        public override int GetHashCode()
        {
            return (m_object == null)? base.GetHashCode() : m_object.GetHashCode();
        }

        public static bool operator == (ObjectReferenceBase<T> lhs, ObjectReferenceBase<T> rhs) {

            object lobj = lhs;
            object robj = rhs;

            if(lobj == null && robj == null) {
                return true;
            }
            if(lobj == null || robj == null) {
                return false;
            }

            return lhs.m_object == rhs.m_object;
        }

        public static bool operator != (ObjectReferenceBase<T> lhs, ObjectReferenceBase<T> rhs) {
            return !(lhs == rhs);
        }
	}

    /**
     * ObjectReference is a container to reference to UnityEngine.Object.
     * ObjectReference is necessary to save reference to UnityEngine.Object instances
     * inside subclass of Node, Modifier and PrefabBuilder. 
     */ 
    [System.Serializable]
    public class ObjectReference : ObjectReferenceBase<UnityEngine.Object> {
        public ObjectReference() : base() {}
        public ObjectReference(ObjectReference r): base(r) {}
        public ObjectReference(UnityEngine.Object obj) : base(obj) {}
    }

    [System.Serializable]
    public class GameObjectReference : ObjectReferenceBase<GameObject> {
        public GameObjectReference() : base() {}
        public GameObjectReference(GameObjectReference r): base(r) {}
        public GameObjectReference(GameObject obj) : base(obj) {}
    }

    [System.Serializable]
    public class MaterialReference : ObjectReferenceBase<Material> {
        public MaterialReference() : base() {}
        public MaterialReference(MaterialReference r): base(r) {}
        public MaterialReference(Material obj) : base(obj) {}
    }

    [System.Serializable]
    public class TextureReference : ObjectReferenceBase<Texture> {
        public TextureReference() : base() {}
        public TextureReference(TextureReference r): base(r) {}
        public TextureReference(Texture obj) : base(obj) {}
    }

    [System.Serializable]
    public class AnimationReference : ObjectReferenceBase<Animation> {
        public AnimationReference() : base() {}
        public AnimationReference(AnimationReference r): base(r) {}
        public AnimationReference(Animation obj) : base(obj) {}
    }

    [System.Serializable]
    public class ShaderReference : ObjectReferenceBase<Shader> {
        public ShaderReference() : base() {}
        public ShaderReference(ShaderReference r): base(r) {}
        public ShaderReference(Shader obj) : base(obj) {}
    }

    [System.Serializable]
    public class PhysicMaterialReference : ObjectReferenceBase<PhysicMaterial> {
        public PhysicMaterialReference() : base() {}
        public PhysicMaterialReference(PhysicMaterialReference r): base(r) {}
        public PhysicMaterialReference(PhysicMaterial obj) : base(obj) {}
    }

    [System.Serializable]
    public class PhysicsMaterial2DReference : ObjectReferenceBase<PhysicsMaterial2D> {
        public PhysicsMaterial2DReference() : base() {}
        public PhysicsMaterial2DReference(PhysicsMaterial2DReference r): base(r) {}
        public PhysicsMaterial2DReference(PhysicsMaterial2D obj) : base(obj) {}
    }

    [System.Serializable]
    public class RenderTextureReference : ObjectReferenceBase<RenderTexture> {
        public RenderTextureReference() : base() {}
        public RenderTextureReference(RenderTextureReference r): base(r) {}
        public RenderTextureReference(RenderTexture obj) : base(obj) {}
    }

    [System.Serializable]
    public class SpriteReference : ObjectReferenceBase<Sprite> {
        public SpriteReference() : base() {}
        public SpriteReference(SpriteReference r): base(r) {}
        public SpriteReference(Sprite obj) : base(obj) {}
    }

    [System.Serializable]
    public class FontReference : ObjectReferenceBase<Font> {
        public FontReference() : base() {}
        public FontReference(FontReference r): base(r) {}
        public FontReference(Font obj) : base(obj) {}
    }

    [System.Serializable]
    public class CubemapReference : ObjectReferenceBase<Cubemap> {
        public CubemapReference() : base() {}
        public CubemapReference(CubemapReference r): base(r) {}
        public CubemapReference(Cubemap obj) : base(obj) {}
    }

    [System.Serializable]
    public class AudioClipReference : ObjectReferenceBase<AudioClip> {
        public AudioClipReference() : base() {}
        public AudioClipReference(AudioClipReference r): base(r) {}
        public AudioClipReference(AudioClip obj) : base(obj) {}
    }

    [System.Serializable]
    public class AudioMixerReference : ObjectReferenceBase<UnityEngine.Audio.AudioMixer> {
        public AudioMixerReference() : base() {}
        public AudioMixerReference(AudioMixerReference r): base(r) {}
        public AudioMixerReference(UnityEngine.Audio.AudioMixer obj) : base(obj) {}
    }

    [System.Serializable]
    public class AvatarMaskReference : ObjectReferenceBase<AvatarMask> {
        public AvatarMaskReference() : base() {}
        public AvatarMaskReference(AvatarMaskReference r): base(r) {}
        public AvatarMaskReference(AvatarMask obj) : base(obj) {}
    }
}
    