using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Lore
{
    public class PrefabManager
    {
        private Dictionary<string, List<ManagedPrefab>> m_ObjectBuffers;
        private Dictionary<string, List<ManagedPrefab>> m_ObjectBuffers2D;

        private Transform m_ParentTransform;


        public PrefabManager()
        {
            m_ObjectBuffers = new Dictionary<string, List<ManagedPrefab>>();
            m_ObjectBuffers2D = new Dictionary<string, List<ManagedPrefab>>();

            m_ParentTransform = new GameObject("ManagedPrefabs").transform;
        }


        public void RegisterPrefab(string name, uint count = 5)
        {
            List<ManagedPrefab> buffer = null;
            if (m_ObjectBuffers.ContainsKey(name))
            {
                buffer = m_ObjectBuffers[name];
            }
            else
            {
                buffer = new List<ManagedPrefab>();
                m_ObjectBuffers.Add(name, buffer);
            }

            GameObject loadedPrefab = Resources.Load<GameObject>(name);
            DebugHelper.Assert(loadedPrefab != null, "Trying to register a prefab that doesn't exist.");

            ManagedPrefab tmpObject = null;
            for (uint i = 0; i < count; i++)
            {
                tmpObject = GameObject.Instantiate(loadedPrefab).GetComponent<ManagedPrefab>();
                DebugHelper.Assert(tmpObject != null, "Prefab missing ManagedPrefab.");
                tmpObject.gameObject.SetActive(false);
                tmpObject.transform.SetParent(m_ParentTransform, false);
                buffer.Add(tmpObject);
            }
        }

        public T RequestObject<T>(string name, uint allowBuffer = 5) where T : ManagedPrefab
        {
            List<ManagedPrefab> buffer = null;
            if (m_ObjectBuffers.ContainsKey(name))
            {
                buffer = m_ObjectBuffers[name];
            }
            else
            {
                buffer = new List<ManagedPrefab>();
                m_ObjectBuffers.Add(name, buffer);
            }

            ManagedPrefab obj = null;
            for (int i = 0; i < buffer.Count; i++)
            {
                if (buffer[i] == null)
                {
                    buffer.RemoveAt(i);
                    i--;
                }
                else if (!buffer[i].prefabUsed)
                {
                    obj = buffer[i];
                    break;
                }
            }

            if (obj != null)
            {
                obj.prefabUsed = true;
                obj.gameObject.SetActive(true);
                obj.OnRequested();
                return obj as T;
            }
            else
            {
                if (allowBuffer == 0)
                    return default(T);

                GameObject loadedPrefab = Resources.Load<GameObject>(name);
                DebugHelper.Assert(loadedPrefab != null, "Trying to register a prefab that doesn't exist.");

                for (uint i = 0; i < allowBuffer; i++)
                {
                    obj = GameObject.Instantiate(loadedPrefab).GetComponent<ManagedPrefab>();
                    DebugHelper.Assert(obj != null, "Prefab missing ManagedPrefab.");
                    obj.gameObject.SetActive(false);
                    obj.transform.SetParent(m_ParentTransform, false);
                    buffer.Add(obj);
                }

                obj.prefabUsed = true;
                obj.gameObject.SetActive(true);
                obj.OnRequested();
                return obj as T;
            }
        }

        public void ReleaseObject(ManagedPrefab obj)
        {
            //remove all mods
            UIManager.i.modManager.ClearObject(obj.gameObject);

            obj.OnReleased();
            obj.prefabUsed = false;
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(m_ParentTransform, false);
        }


        public void RegisterPrefab2D(string name, RectTransform parent, uint count = 5)
        {
            List<ManagedPrefab> buffer = null;
            if (m_ObjectBuffers2D.ContainsKey(name))
            {
                buffer = m_ObjectBuffers2D[name];
            }
            else
            {
                buffer = new List<ManagedPrefab>();
                m_ObjectBuffers2D.Add(name, buffer);
            }

            GameObject loadedPrefab = Resources.Load<GameObject>(name);
            DebugHelper.Assert(loadedPrefab != null, "Trying to register a prefab that doesn't exist.");

            ManagedPrefab tmpObject = null;
            for (uint i = 0; i < count; i++)
            {
                tmpObject = GameObject.Instantiate(loadedPrefab).GetComponent<ManagedPrefab>();
                DebugHelper.Assert(tmpObject != null, "Prefab missing ManagedPrefab.");
                tmpObject.gameObject.SetActive(false);
                tmpObject.transform.SetParent(parent, false);
                buffer.Add(tmpObject);
            }
        }

        public T RequestObject2D<T>(string name, RectTransform parent, uint allowBuffer = 5) where T : ManagedPrefab
        {
            List<ManagedPrefab> buffer = null;
            if (m_ObjectBuffers2D.ContainsKey(name))
            {
                buffer = m_ObjectBuffers2D[name];
            }
            else
            {
                buffer = new List<ManagedPrefab>();
                m_ObjectBuffers2D.Add(name, buffer);
            }

            ManagedPrefab obj = null;
            for (int i = 0; i < buffer.Count; i++)
            {
                if (buffer[i] == null)
                {
                    buffer.RemoveAt(i);
                    i--;
                }
                else if (!buffer[i].prefabUsed)
                {
                    obj = buffer[i];
                    break;
                }
            }

            if (obj != null)
            {
                obj.prefabUsed = true;
                obj.gameObject.SetActive(true);
                obj.OnRequested();
                return obj as T;
            }
            else
            {
                if (parent == null || allowBuffer == 0)
                    return default(T);

                GameObject loadedPrefab = Resources.Load<GameObject>(name);
                DebugHelper.Assert(loadedPrefab != null, "Trying to register a prefab that doesn't exist.");

                for (uint i = 0; i < allowBuffer; i++)
                {
                    obj = GameObject.Instantiate(loadedPrefab).GetComponent<ManagedPrefab>();
                    DebugHelper.Assert(obj != null, "Prefab missing ManagedPrefab.");
                    obj.gameObject.SetActive(false);
                    obj.transform.SetParent(parent, false);
                    buffer.Add(obj);
                }

                obj.prefabUsed = true;
                obj.gameObject.SetActive(true);
                obj.OnRequested();
                return obj as T;
            }
        }

        public void ReleaseObject2D(ManagedPrefab obj)
        {
            //remove all mods
            UIManager.i.modManager.ClearObject(obj.gameObject);

            obj.OnReleased();
            obj.prefabUsed = false;
            obj.gameObject.SetActive(false);
        }
    }
}


