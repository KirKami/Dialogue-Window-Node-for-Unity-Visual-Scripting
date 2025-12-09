using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Localization.Settings;

/// <summary>
/// Visual Scripting Node that calls to Dialogue Window system to show Dialogue Window with a text
/// </summary>
[UnitTitle("Dialogue Window"), UnitCategory("Dialogue Nodes"), TypeIcon(typeof(GUI))]
public class DialogueWindowNode : Unit
{
    [DoNotSerialize] // No need to serialize ports.
    public ControlInput Enter; //Adding the ControlInput port variable

    [DoNotSerialize] // No need to serialize ports.
    public ControlOutput Exit;//Adding the ControlOutput port variable.

    public ValueInput NameplateTable; // Adding the ValueInput variable
    public ValueInput Nameplate; // Adding the ValueInput variable

    public ValueInput TextTable; // Adding the ValueInput variable
    public ValueInput Text; // Adding the ValueInput variable

    protected override void Definition()
    {
        //Run as Coroutine to wait until Dialogue Window closes before continuing script.
        Enter = ControlInputCoroutine(nameof(Enter), Await);
        Exit = ControlOutput(nameof(Exit));

        Succession(Enter, Exit);

        //Localized strings are used, but serialization of LocalizedString by Visual Scripting requires custom inspector serialization.
        //Instead inspector of LocalizedString is stripped down and raw links are used.
        //Can easily be changed to just String or custom solution.
        NameplateTable = ValueInput<string>("Nameplate Localization Table", string.Empty);
        Nameplate = ValueInput<string>("Nameplate Entry", string.Empty);
        TextTable = ValueInput<string>("Dialogue Text Localization Table", string.Empty);
        Text = ValueInput<string>("Dialogue Text Entry", string.Empty);
    }

    protected IEnumerator Await(Flow flow)
    {
        //Resolve LocalizedStrings in runtime
        var nameplateString 
            = LocalizationSettings.StringDatabase.GetLocalizedString
            (
                flow.GetValue<string>(NameplateTable), 
                flow.GetValue<string>(Nameplate)
            );

        var textString
            = LocalizationSettings.StringDatabase.GetLocalizedString
            (
                flow.GetValue<string>(TextTable),
                flow.GetValue<string>(Text)
            );

        //Wait unitl bool "completed" becomes true.
        //It becomes true when action "onClose" is executed when Dialogue Window is closed.
        bool completed = false;
        DialogueWindow.ShowDialogue(
            nameplateString,
            textString, 
            onShow: null,
            onClose: () => completed = true);

        //Wait for Dialogue Window closing.
        yield return new WaitUntil(() => completed);

        //Run next node.
        yield return Exit;
    }
}
