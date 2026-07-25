using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AnimTextWrite : MonoBehaviour
{
    public TextMeshProUGUI[] text;

    //JUST SCRIPT WRITE THE TEXT, LAUNCHED BY THE ANIMATION
    public void StartText1Write()
    {
        TextWriter.AddWriter_Static(text[0], UIGlobal.instance.WriteSounds[1], false, "fd sq f qsd f sq df frty bez dsq f d sqf kuyr  piu sqdf d sq f qs jyt lfdsjzpok zokk", .02f, true, true, null);
    }

    public void StartText2Write()
    {
        TextWriter.AddWriter_Static(text[1], UIGlobal.instance.WriteSounds[1], false, "sfdsfdsf fdsfds\nf fdsfds fdsfdssdf fd\nsf dfsdsf ds\nf dsf dfsf ds fdg fdgf\ng hghre\ng eryetertre retr hre\n hgfhgfhgfhregrereert ryurth\nj yturyurye ytrurtyrytrytr rtloup\nulutj omuiktyjtryu r uli um yktyjrj\nliyltuktyk uuylukt\nlulytkyt liyukyt luy\nkjryjy ylyulut ur\n kuylktkry jrjru r rt lylut\nrju rtye dhgfgf lyluyty ukuy\nrurt urty ry tr\nluyktyj r ety tru ktt\nlyktj rtruytytyty tjrty y\nykukt e ytry y yrutry rty rtyrty rty rtytr ytjy r ukuyliluiltkt jrghgfhgf liluy t\nkty ryjy tuyt kt\nktktru ruyt uyouitu rr\nluluylyu\n\nlyuk tyjr ur\nly u ry r uryyrt\nluykt rurt", .002f, true, true, StartText3Write);
    }

    public void StartText3Write()
    {
        TextWriter.AddWriter_Static(text[2], UIGlobal.instance.WriteSounds[1], false, "fdgfdgfdg jgfd gfex", .01f, true, true, StartText4Write);
    }

    public void StartText4Write()
    {
        TextWriter.AddWriter_Static(text[3], UIGlobal.instance.WriteSounds[1], false, "gfd gfzrezrezr grfe", .03f, true, true, StartText5Write);
    }

    public void StartText5Write()
    {
        TextWriter.AddWriter_Static(text[4], UIGlobal.instance.WriteSounds[1], false, "14°", .01f, true, true, StartText6Write);
    }

    public void StartText6Write()
    {
        TextWriter.AddWriter_Static(text[5], UIGlobal.instance.WriteSounds[1], false, "sfdsfdsf fdsfds\nf fdsfds fdsfdssdf fd\nsf dfsdsf ds\nf dsf dfsf ds fdg fdgf\ng hghre\ng eryetertre retr hre\n hgfhgfhgfhregrereert ryurth\nj yturyurye ytrurtyrytrytr rtloup\nulutj omuiktyjtryu r uli um yktyjrj\nliyltuktyk uuylukt\nlulytkyt liyukyt luy\nkjryjy ylyulut ur\n kuylktkry jrjru r rt lylut\nrju rtye dhgfgf lyluyty ukuy\nrurt urty ry tr\nluyktyj r ety tru ktt\nlyktj rtruytytyty tjrty y\nykukt e ytry y yrutry rty rtyrty rty rtytr ytjy r ukuyliluiltkt jrghgfhgf liluy t\nkty ryjy tuyt kt\nktktru ruyt uyouitu rr\nluluylyu\n\nlyuk tyjr ur\nly u ry r uryyrt\nluykt rurt", .002f, true, true, null);
    }

    public void resetText()
    {
        foreach(TextMeshProUGUI t in text)
        {
            t.text = "";
        }
    }
}