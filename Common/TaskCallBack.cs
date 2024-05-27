using System;
using System.Threading.Tasks;

public class TaskCallBack
{
    public static void RunTask(Action beforeAction, Action afterAction)
    {
        Task.Run(() =>
        {
            try
            {
                beforeAction();
            }
            catch (Exception e) { UnityEngine.Debug.LogException(e); }
        })
        .ContinueWith(task =>
        {
            afterAction();
        });
    }
    public static void RunTaskMain(Action beforeAction, Action afterAction)
    {
        Task.Run(() =>
        {
            try
            {
                beforeAction();
            }
            catch (Exception e) { UnityEngine.Debug.LogException(e); }
        })
        .ContinueWith(task =>
        {
            afterAction();
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }
}
