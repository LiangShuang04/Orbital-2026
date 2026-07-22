using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DisplayTimeDate : MonoBehaviour
{
    public TextMeshProUGUI displayHour;
    public TextMeshProUGUI displayDate;
    public TextMeshProUGUI displayMonth;
    public TextMeshProUGUI displayYear;

    int hours;
    int minutes;
    int seconds;
    int date;
    int month;
    int year;

    string monthString;

    //JUST SHOW THE TIME IN TOP MENU
    void Update()
    {
        hours = System.DateTime.Now.Hour;
        minutes = System.DateTime.Now.Minute;
        seconds = System.DateTime.Now.Second;
        displayHour.text = "" + hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");

        date = System.DateTime.Now.Day;
        month = System.DateTime.Now.Month;
        year = System.DateTime.Now.Year;
        if(month == 1)
        {
            monthString = "Jan";
        }
        else if(month == 2)
        {
            monthString = "Feb";
        }
        else if (month == 3)
        {
            monthString = "Mar";
        }
        else if (month == 4)
        {
            monthString = "Apr";
        }
        else if (month == 5)
        {
            monthString = "May";
        }
        else if (month == 6)
        {
            monthString = "Jun";
        }
        else if (month == 7)
        {
            monthString = "Jul";
        }
        else if (month == 8)
        {
            monthString = "Aug";
        }
        else if (month == 9)
        {
            monthString = "Sep";
        }
        else if (month == 10)
        {
            monthString = "Oct";
        }
        else if (month == 11)
        {
            monthString = "Nov";
        }
        else if (month == 12)
        {
            monthString = "Dec";
        }
        displayDate.text = "" + date;
        displayMonth.text = "" + monthString;
        displayYear.text = "" + year;
    }
}
