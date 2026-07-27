using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType(typeof(T)) as T;

                if (instance == null)
                {
                    instance = new GameObject().AddComponent<T>();
                    instance.gameObject.name = instance.GetType().Name;
                }
            }
            return instance;
        }
    }

    public void Reset()
    {
        instance = null;
    }

    public static bool Exists()
    {
        return (instance != null);
    }
}
