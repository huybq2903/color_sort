/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-09
     */


using UnityEngine;

namespace Falcon.Modules.Core.Utils.Time.Demo
{
	using System;
	using Falcon.Modules.Core.Utils.Time.Runtime;
	using TMPro;
	using Time = UnityEngine.Time;

	public class AutoCountdownDemo : MonoBehaviour
    {
	    public TMP_Text text1;
	    public long     futureTimeFromNowExpand = 7200;
	    
	    public TMP_Text text2;
	    public long     periodOfTime = 7200;

	    private AutoCountdownTimer _autoCountdownTimer1;
	    private AutoCountdownTimer _autoCountdownTimer2;

	    private void OnValidate()
	    {
		    if (futureTimeFromNowExpand < 0)
		    {
			    futureTimeFromNowExpand = 180;
		    }
		    
		    if (periodOfTime < 0)
		    {
			    periodOfTime = 180;
		    }
	    }

	    private void Start()
	    {
		    FutureTimestamp();
		    PeriodOfTime();
	    }
	    
	    private void FutureTimestamp()
	    {
		    _autoCountdownTimer1 = new AutoCountdownTimer(9999, 9999, 9999);

		    var utcNowTimeStamp = TimeUtils.GetCurrentTimestampInSecondsUTC();
		    _autoCountdownTimer1.StartCountDownFromFutureTimestampUTC(text1, utcNowTimeStamp + futureTimeFromNowExpand, true, seconds =>
		    {
			    var timeSpan = TimeSpan.FromSeconds(seconds);
			    text1.text = timeSpan.ToString(@"hh\:mm\:ss");
		    });
	    }
	    
	    private void PeriodOfTime()
	    {
		    _autoCountdownTimer2 = new AutoCountdownTimer(9999, 9999, 9999);

		    _autoCountdownTimer2.StartCountDownFromPeriodTime(text2, periodOfTime, false, seconds =>
		    {
			    var timeSpan = TimeSpan.FromSeconds(seconds);
			    text2.text = timeSpan.ToString(@"hh\:mm\:ss");
		    });
	    }

	    public void Stop()
	    {
		    _autoCountdownTimer1.Stop();
		    _autoCountdownTimer2.Stop();
	    }

	    public void TogglePause()
	    {
		    if (Time.timeScale > 0)
		    {
			    Time.timeScale = 0;
		    }
		    else
		    {
			    Time.timeScale = 1;
		    }
	    }
    }
}
