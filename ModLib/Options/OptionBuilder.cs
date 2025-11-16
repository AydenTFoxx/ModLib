using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace ModLib.Options;

/// <summary>
///     Helper class for building <c>OpTab</c>s with a variety of chain-able methods.
/// </summary>
/// <remarks>To return the generated <c>OpTab</c> object, use <see cref="Build()"/>.</remarks>
public class OptionBuilder(OpTab opTab)
{
    private static Vector2 DefaultOrigin = new(100f, 400f);

    private readonly List<UIelement> elementsToAdd = [];
    private Vector2 vector2 = DefaultOrigin;

    /// <summary>
    ///     Initializes a new <c>OptionBuilder</c> instance for creating option tabs.
    /// </summary>
    /// <param name="owner">The <c>OptionInterface</c> who will own the resulting <c>OpTab</c> instance.</param>
    /// <param name="tabName">The name of the tab itself, displayed on the left side of the menu; Only visible with two or more tabs.</param>
    /// <param name="colors">
    ///     The color values to be used by the <c>OpTab</c> instance.
    ///     Colors are retrieved by index and applied to relevant fields in alphabetical order.
    /// </param>
    public OptionBuilder(OptionInterface owner, string tabName, params Color[] colors)
        : this(
            new OpTab(owner, tabName)
            {
                colorButton = GetColorOrDefault(colors, 0),
                colorCanvas = GetColorOrDefault(colors, 1)
            }
        )
    {
    }

    /// <summary>
    ///     Returns the generated <c>OpTab</c> object with the applied options of previous methods.
    /// </summary>
    /// <returns>The generated <c>OpTab</c> instance.</returns>
    public OpTab Build()
    {
        opTab.AddItems([.. elementsToAdd]);

        elementsToAdd.Clear();

        return opTab;
    }

    /// <inheritdoc cref="AddCheckBoxOption(string, Configurable{bool}, out OpLabel, out OpCheckBox, Color[])"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public OptionBuilder AddCheckBoxOption(string text, Configurable<bool> configurable, params Color[] colors) =>
        AddCheckBoxOption(text, configurable, out _, out _, colors);

    /// <inheritdoc cref="AddCheckBoxOption(string, Configurable{bool}, out OpLabel, out OpCheckBox, Color[])"/>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OptionBuilder AddCheckBoxOption(string text, Configurable<bool> configurable, out OpCheckBox checkBox, params Color[] colors) =>
        AddCheckBoxOption(text, configurable, out _, out checkBox, colors);

    /// <summary>
    ///     Adds a new <c>OpCheckBox</c> to the <c>OpTab</c> instance, with a descriptive <c>OpLabel</c> after it.
    /// </summary>
    /// <param name="text">The check box's label. Will be displayed right after the box itself.</param>
    /// <param name="configurable">The <c>Configurable</c> this check box will be bound to.</param>
    /// <param name="label">The generated <c>OpLabel</c> for this option.</param>
    /// <param name="checkBox">The generated <c>OpCheckBox</c> for this option.</param>
    /// <param name="colors">
    ///     The color values to be used by the <c>OpLabel</c> and <c>OpCheckBox</c> instance.
    ///     Colors are retrieved by index and applied to relevant fields in alphabetical order.
    /// </param>
    /// <returns>The <c>OptionBuilder</c> object.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public OptionBuilder AddCheckBoxOption(string text, Configurable<bool> configurable, out OpLabel label, out OpCheckBox checkBox, params Color[] colors)
    {
        UIelement[] elements = [
            label = new OpLabel(vector2 + new Vector2(40f, 0f), new Vector2(100f, 24f), text)
            {
                description = configurable.info.description,
                alignment = FLabelAlignment.Left,
                verticalAlignment = OpLabel.LabelVAlignment.Center,
                color = GetColorOrDefault(colors, 0)
            },
            checkBox = new OpCheckBox(configurable, vector2)
            {
                description = configurable.info.description,
                colorEdge = GetColorOrDefault(colors, 1),
                colorFill = GetColorOrDefault(colors, 2, MenuColorEffect.rgbBlack)
            }
        ];

        elementsToAdd.AddRange(elements);

        vector2.y -= 32f;

        return this;
    }

    /// <inheritdoc cref="AddComboBoxOption(string, Configurable{string}, out OpLabel, out OpComboBox, float, Color[])"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public OptionBuilder AddComboBoxOption(string text, Configurable<string> configurable, float width = 200, params Color[] colors) =>
        AddComboBoxOption(text, configurable, out _, out _, width, colors);

    /// <inheritdoc cref="AddComboBoxOption(string, Configurable{string}, out OpLabel, out OpComboBox, float, Color[])"/>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OptionBuilder AddComboBoxOption(string text, Configurable<string> configurable, out OpComboBox comboBox, float width = 200, params Color[] colors) =>
        AddComboBoxOption(text, configurable, out _, out comboBox, width, colors);

    /// <summary>
    ///     Adds a new <c>OpComboBox</c> to the <c>OpTab</c> instance, with a descriptive <c>OpLabel</c> after it.
    /// </summary>
    /// <param name="text">The combo box's label. Will be displayed right after the box itself.</param>
    /// <param name="configurable">The <c>Configurable</c> this combo box will be bound to.</param>
    /// <param name="label">The generated <c>OpLabel</c> for this option.</param>
    /// <param name="comboBox">The generated <c>OpComboBox</c> for this option.</param>
    /// <param name="width">The width of the combo box element.</param>
    /// <param name="colors">
    ///     The color values to be used by the <c>OpLabel</c> and <c>OpComboBox</c> instance.
    ///     Colors are retrieved by index and applied to relevant fields in alphabetical order.
    /// </param>
    /// <returns>The <c>OptionBuilder</c> object.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public OptionBuilder AddComboBoxOption(string text, Configurable<string> configurable, out OpLabel label, out OpComboBox comboBox, float width = 200, params Color[] colors)
    {
        UIelement[] elements = [
            label = new OpLabel(vector2 + new Vector2(40f, 0f), new Vector2(100f, 24f), text)
            {
                description = configurable.info.description,
                alignment = FLabelAlignment.Left,
                verticalAlignment = OpLabel.LabelVAlignment.Center,
                color = GetColorOrDefault(colors, 0)
            },
            comboBox = new OpComboBox(configurable, vector2 + new Vector2(180f, 00f), width)
            {
                description = configurable.info.description,
                colorEdge = GetColorOrDefault(colors, 1),
                colorFill = GetColorOrDefault(colors, 2, MenuColorEffect.rgbBlack)
            }
        ];

        elementsToAdd.AddRange(elements);

        vector2.y -= 32f;

        return this;
    }

    /// <inheritdoc cref="AddSliderOption(string, Configurable{int}, out OpLabel, out OpSlider, float, bool, Color[])"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public OptionBuilder AddSliderOption(string text, Configurable<int> configurable, float multi = 1f, bool vertical = false, params Color[] colors) =>
        AddSliderOption(text, configurable, out _, out _, multi, vertical, colors);

    /// <inheritdoc cref="AddSliderOption(string, Configurable{int}, out OpLabel, out OpSlider, float, bool, Color[])"/>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OptionBuilder AddSliderOption(string text, Configurable<int> configurable, out OpSlider slider, float multi = 1f, bool vertical = false, params Color[] colors) =>
        AddSliderOption(text, configurable, out _, out slider, multi, vertical, colors);

    /// <summary>
    ///     Adds a new <c>OpSlider</c> to the <c>OpTab</c> instance, with a descriptive <c>OpLabel</c> before it.
    /// </summary>
    /// <param name="text">The slider's label. Will be displayed right before the slider itself.</param>
    /// <param name="configurable">The <c>Configurable</c> this slider will be bound to.</param>
    /// <param name="label">The generated <c>OpLabel</c> for this option.</param>
    /// <param name="slider">The generated <c>OpSlider</c> for this option.</param>
    /// <param name="multi">A multiplier for the slider's size.</param>
    /// <param name="vertical">If this slider should be vertical.</param>
    /// <param name="colors">
    ///     The color values to be used by the <c>OpLabel</c> and <c>OpSlider</c> instance.
    ///     Colors are retrieved by index and applied to relevant fields in alphabetical order.
    /// </param>
    /// <returns>The <c>OptionBuilder</c> object.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public OptionBuilder AddSliderOption(string text, Configurable<int> configurable, out OpLabel label, out OpSlider slider, float multi = 1f, bool vertical = false, params Color[] colors)
    {
        UIelement[] elements = [
            label = new OpLabel(vector2 + new Vector2(40f, 0f), new Vector2(100f, 24f), text)
            {
                description = configurable.info.description,
                verticalAlignment = OpLabel.LabelVAlignment.Center,
                color = GetColorOrDefault(colors, 0)
            },
            slider = new OpSlider(configurable, vector2 + new Vector2(200f, 0f), multi, vertical)
            {
                description = configurable.info.description,
                colorEdge = GetColorOrDefault(colors, 1),
                colorFill = GetColorOrDefault(colors, 2, MenuColorEffect.rgbBlack),
                colorLine = GetColorOrDefault(colors, 3, MenuColorEffect.rgbVeryDarkGrey)
            }
        ];

        elementsToAdd.AddRange(elements);

        vector2.y -= 32f;

        return this;
    }

    /// <inheritdoc cref="AddFloatSliderOption(string, Configurable{float}, out OpLabel, out OpFloatSlider, int, byte, bool, Color[])"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public OptionBuilder AddFloatSliderOption(string text, Configurable<float> configurable, int length = 80, byte decimalNum = 1, bool vertical = false, params Color[] colors) =>
        AddFloatSliderOption(text, configurable, out _, out _, length, decimalNum, vertical, colors);

    /// <inheritdoc cref="AddFloatSliderOption(string, Configurable{float}, out OpLabel, out OpFloatSlider, int, byte, bool, Color[])"/>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OptionBuilder AddFloatSliderOption(string text, Configurable<float> configurable, out OpFloatSlider slider, int length = 80, byte decimalNum = 1, bool vertical = false, params Color[] colors) =>
        AddFloatSliderOption(text, configurable, out _, out slider, length, decimalNum, vertical, colors);

    /// <summary>
    ///     Adds a new <c>OpFloatSlider</c> to the <c>OpTab</c> instance, with a descriptive <c>OpLabel</c> before it.
    /// </summary>
    /// <param name="text">The slider's label. Will be displayed right before the slider itself.</param>
    /// <param name="configurable">The <c>Configurable</c> this slider will be bound to.</param>
    /// <param name="label">The generated <c>OpLabel</c> for this option.</param>
    /// <param name="slider">The generated <c>OpFloatSlider</c> for this option.</param>
    /// <param name="length">The length of the slider itself.</param>
    /// <param name="decimalNum"></param>
    /// <param name="vertical">If this slider should be vertical.</param>
    /// <param name="colors">
    ///     The color values to be used by the <c>OpLabel</c> and <c>OpFloatSlider</c> instance.
    ///     Colors are retrieved by index and applied to relevant fields in alphabetical order.
    /// </param>
    /// <returns>The <c>OptionBuilder</c> object.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public OptionBuilder AddFloatSliderOption(string text, Configurable<float> configurable, out OpLabel label, out OpFloatSlider slider, int length = 80, byte decimalNum = 1, bool vertical = false, params Color[] colors)
    {
        UIelement[] elements = [
            label = new OpLabel(vector2 + new Vector2(40f, 0f), new Vector2(100f, 24f), text)
            {
                description = configurable.info.description,
                verticalAlignment = OpLabel.LabelVAlignment.Center,
                color = GetColorOrDefault(colors, 0)
            },
            slider = new OpFloatSlider(configurable, vector2 + new Vector2(200f, 0f), length, decimalNum, vertical)
            {
                description = configurable.info.description,
                colorEdge = GetColorOrDefault(colors, 1),
                colorFill = GetColorOrDefault(colors, 2, MenuColorEffect.rgbBlack),
                colorLine = GetColorOrDefault(colors, 3, MenuColorEffect.rgbVeryDarkGrey)
            }
        ];

        elementsToAdd.AddRange(elements);

        vector2.y -= 32f;

        return this;
    }

    /// <summary>
    ///     Adds extra space before the next object added.
    /// </summary>
    /// <param name="padding">The amount of padding to be added.</param>
    /// <returns>The <c>OptionBuilder</c> object.</returns>
    public OptionBuilder AddPadding(Vector2 padding)
    {
        vector2 -= padding;

        return this;
    }

    /// <inheritdoc cref="AddText(string, Vector2, out OpLabel, bool, Color?)"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public OptionBuilder AddText(string text, Vector2 size, bool bigText = false, Color? color = default) =>
        AddText(text, size, out _, bigText, color);

    /// <summary>
    ///     Adds a new <c>OpLabel</c> to the <c>OpTab</c> instance.
    /// </summary>
    /// <param name="text">The text to be rendered.</param>
    /// <param name="size">The size of the label element.</param>
    /// <param name="label">The generated <c>OpLabel</c>.</param>
    /// <param name="bigText">If this text should be rendered larger than usual.</param>
    /// <param name="color">The color of the text.</param>
    /// <returns>The <c>OptionBuilder</c> object.</returns>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public OptionBuilder AddText(string text, Vector2 size, out OpLabel label, bool bigText = false, Color? color = default)
    {
        UIelement[] UIarrayOptions =
        [
            label = new OpLabel(vector2 + new Vector2(100f + size.x, 10f), size, text, FLabelAlignment.Center, bigText)
            {
                verticalAlignment = OpLabel.LabelVAlignment.Center,
                color = color ?? MenuColorEffect.rgbMediumGrey
            }
        ];

        opTab.AddItems(UIarrayOptions);

        vector2.y -= size.y + 30f;

        return this;
    }

    /// <summary>
    ///     Retrieves the origin at which elements will be added for this <see cref="OptionBuilder"/> instance.
    /// </summary>
    /// <returns>The position at which elements will be added for this builder instance.</returns>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public Vector2 GetOrigin() => vector2;

    /// <summary>
    ///     Sets the origin at which elements will be added for this <see cref="OptionBuilder"/> instance.
    /// </summary>
    /// <param name="origin">The position to be set.</param>
    /// <returns>The <c>OptionBuilder</c> object.</returns>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public OptionBuilder SetOrigin(Vector2 origin)
    {
        vector2 = origin;

        return this;
    }

    /// <summary>
    ///     Resets the origin at which elements are added for this <see cref="OptionBuilder"/> instance.
    /// </summary>
    /// <returns>The <c>OptionBuilder</c> object.</returns>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public OptionBuilder ResetOrigin()
    {
        vector2 = DefaultOrigin;

        return this;
    }

    /// <summary>
    ///     Adds one or more <see cref="UIelement"/> objects to the <c>OpTab</c> instance.
    /// </summary>
    /// <param name="elements">The elements to be added.</param>
    /// <returns>The <c>OptionBuilder</c> object.</returns>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public OptionBuilder AddElements(params UIelement[] elements)
    {
        elementsToAdd.AddRange(elements);

        return this;
    }

    /// <summary>
    ///     Retrieves a color from the given array, a fallback if provided, or the default Rain World color for menu elements (rgbMediumGrey) if neither are provided.
    /// </summary>
    /// <param name="colors">The color array to search for a given color.</param>
    /// <param name="index">The index of the color to be retrieved.</param>
    /// <param name="fallback">A fallback color to use if the given index does not have a value.</param>
    /// <returns>A <c>Color</c> instance for usage by menu elements.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Color GetColorOrDefault(Color[] colors, int index, Color fallback = default)
    {
        Color color = colors.ElementAtOrDefault(index);

        if (color == default)
        {
            color = fallback != default
                ? fallback
                : MenuColorEffect.rgbMediumGrey;
        }

        return color;
    }
}