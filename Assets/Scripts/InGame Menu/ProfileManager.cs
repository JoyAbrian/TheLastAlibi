using TMPro;
using UnityEngine;

public class ProfileManager : MonoBehaviour
{
    public TextMeshProUGUI nameText, idText, regionText, zipText;

    private string[] id = { "HAL9-000-0111", "FROD-O000-BGIN", "WLLY-WNK4-CHOC", "DCOT-RSTR-WHOV", "MTRX-BLUP-ILLZ" };
    private string[] region = { "Riverbend Plain Lands", "Radiant Peak Lands", "Diamond Oak Plateau", "Deepwood Outpost Provinces", "Zephyr Bluff Lands", "Zinc Bay Lowlands", "Redwood Forest Quadrangle", "Ravine Fall Quarter" };
    private string[] zip = { "90210", "11380", "221B0", "00700", "42000", "55500", "93400", "69420" };

    private void Start()
    {
        if (SuspectAIManager.GeneratedProfile != null)
        {
            DisplayProfile(SuspectAIManager.GeneratedProfile);
        }
    }

    public void DisplayProfile(SuspectProfile profile)
    {
        if (profile == null)
            return;

        nameText.text = "Name: " + profile.name;
        idText.text = "Citizen ID: " + id[Random.Range(0, id.Length)];
        regionText.text = "Region: " + region[Random.Range(0, region.Length)];
        zipText.text = "Zip Code: " + zip[Random.Range(0, zip.Length)];
    }
}