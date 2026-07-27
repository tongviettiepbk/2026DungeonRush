using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectPooling<T> where T : class, new()
{
    public int Count
    {
        get { return m_objectStack.Count; }
    }

    private Stack<T> m_objectStack;

    public ObjectPooling(int initialBufferSize = 8)
    {
        m_objectStack = new Stack<T>(initialBufferSize);
    }

    public T New()
    {
        T t = null;

        if (m_objectStack.Count > 0)
        {
            t = m_objectStack.Pop();
        }

        return t;
    }

    public void Store(T obj)
    {
        if (Contains(obj))
        {
            return;
        }

        m_objectStack.Push(obj);
    }

    public bool Contains(T obj)
    {
        return m_objectStack.Contains(obj);
    }

    public void Clear()
    {
        while (Count > 8)
        {
            m_objectStack.Pop();
        }

        m_objectStack.TrimExcess();
    }
}