using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Ionic.Zip;
using System.Text;
namespace App.Core
{
    public class Util
    {
        public class delayedTask
        {
            public int timeToBeDone;
            public Action act;
        }
        private List<GameObject> allObjects = new List<GameObject>();
        static List<delayedTask> delayedTasks = new List<delayedTask>();

        public static void go(int delay_, Action act_)
        {
            delayedTasks.Add(new delayedTask
            {
                act = act_,
                timeToBeDone = delay_ + TimePassed(),
            });
        }

        public static void notGo(Action act_)
        {
            List<delayedTask> rem = new List<delayedTask>();
            for (int i = 0; i < delayedTasks.Count; i++)
            {
                if (delayedTasks[i].act == act_)
                {
                    rem.Add(delayedTasks[i]);
                }
            }
            for (int i = 0; i < rem.Count; i++)
            {
                delayedTasks.Remove(rem[i]);
            }
            rem.Clear();
        }

        public static void clear()
        {
            delayedTask remove = null;
            while (true)
            {
                remove = null;
                for (int i = 0; i < delayedTasks.Count; i++)
                {
                    if (TimePassed() > delayedTasks[i].timeToBeDone)
                    {
                        remove = delayedTasks[i];
                        try
                        {
                            remove.act();
                        }
                        catch (System.Exception e)
                        {
                            UnityEngine.Debug.Log(e);
                        }
                        break;
                    }
                }
                if (remove != null)
                {
                    delayedTasks.Remove(remove);
                }
                else
                {
                    break;
                }
            }
        }
        
        public static int TimePassed()
        {
            return (int)(Time.time * 1000f);
        }
        
        void loadResource(GameObject g)
        {
            try
            {
                GameObject obj = GameObject.Instantiate(g) as GameObject;
                obj.SetActive(false);
                allObjects.Add(obj);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.Log(e);
            }
        }
    }
}