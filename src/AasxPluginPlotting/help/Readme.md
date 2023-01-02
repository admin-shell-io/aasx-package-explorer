# Plugin for plotting

## Overview

### General

The plugin "AasxPluginPlotting" adds (live) plotting capabilities for
the AASX Package Explorer. The plugin needs to be loaded by the main
application either via automatic plug-in loading via `PluginDir` 
option or by explicit configuration of the plugin location:

```
  [..]
  "PluginDll": [
    {
      "Path": "..\\..\\..\\..\\..\\AasxPluginPlotting\\bin\\
               Debug\\net472\\AasxPluginPlotting.dll",
      "Args": []
    }
  ],
  [..]
```

Valid presence of the plugin can be checked by inspecting the log
of AASX Package Explorer (lower/ right corner of the window).

### Enabling of plugin for a specific Submodel

As with other plugins, the plugin is only enabled by the AASX Package
Explorer, if the `semanticId` of the Submodel to be displayed in
options for the plugin. This is done by modifying the optionsfile
`AasxPluginPlotting.options.json` accordingly. It may list:

```
{
  "Records": [
    {
      "AllowSubmodelSemanticId": [
        [..]
        {
            "type": "Submodel",
            "local": false,
            "value": "https://admin-shell.io/sandbox/pi40/
                      CarbonMonitoring/1/0",
            "index": 0,
            "idType": "IRI"
        }
        [..]
      ]
    }
  ]
}
```

### Three functionalities for live Submodels

Multiple functionalities might contribute together to the experience
of a live plotting:

1. Animated values
2. Fixed-len plot buffers
3. Time-series plot items

The first functionality is atually a functionality of the main 
application AASX Package Explorer but not of the plug-in. However,
some controlling Extension `Animate.Args` could attached to 
SubmodelElements to animate value e.g. along a sine curve. 
Changes to the value will also send AAS events `UpdateValue`. 
This allows nice mock-ups and helps developing/ debugging a 
configuration.

The second and third functionality are both provided by the 
plugin and have quite different nature.

The fixed-len buffers are controlled via Extensions `Plotting.Args`
and select individual, independent `SubmodelElements` to be 
presented as tiled/ tabled values or as plot items. Multiple plot
items can be arranged into one plot. The plot item always represents
the actual, single SubmodelElement but can be buffered over time.
Re-loading the plugin will clear the buffer; buffer length is 
limited to a fixed value. Samples will taken to the buffer either
by a certain time interval or by an AAS event `UpdateValue`. In
this sense, these buffer could be sample-precisely synchronized to
a server, as well.

The Time-series plot items are mostly controlled by the structure
and semanticIds of the Submodel time series. Only minor graphical
hints are given by Extensions `TimeSeries.Args`. As the data
is kept in SME structures and may be hold by a server, re-loading
of the plugin will also keep the display identical, as no buffers
are cleared. AAS events `StructuralChange`and `UpdateValue`
will update the structure of the time series and will lead to 
redisplay/ update of the plots.

## Animate.Args

### Definitions

```
public class AnimateArgs
{
    public enum TypeDef { None, Sin, Cos, Saw, Square }

    /// <summary>
    /// Type of the mapping function.
    /// </summary>
    public TypeDef type;

    /// <summary>
    /// Frequency. Multiplier to the input of the mapping function. 
    /// Normalized frequency is 1.0 seconds.
    /// </summary>
    public double freq = 0.1;

    /// <summary>
    /// Scale. Multiplier to the output of the mapping function. 
    /// Default is +/- 1.0.
    /// </summary>
    public double scale = 1.0;

    /// <summary>
    /// Offset to the scaled output of the mapping function. 
    /// Default is 0.0.
    /// </summary>
    public double ofs = 0.0;

    /// <summary>
    /// Specifies the timer interval in milli-seconds. 
    /// Minimum value 100ms.
    /// Applicable on: Submodel
    /// </summary>
    public int timer = 1000;
}
```

### Working sample

Extension name: `Animate.Args`

Extension value: 
`{ type: "Sin", ofs: 230.0, scale: 10.0, freq: 0.05, timer: 500 }`

Each 500ms, a new value will be assigned to the attributed 
SubmodelElement, following a sine-function, ranging from 220 to 240 
in value. The sine frequency will by 1 / 0.05, that is, 20 seconds.

## Plotting.Args and TimeSeries.Args

### Definitions

The following definitions are shared between both functionalities.

```
public class PlotArguments
{
    /// <summary>
    /// Display title of the respective entity to be shown in the panel.
    /// </summary>
    public string title;

    /// <summary>
    /// Symbolic name of a group, a plot shall assigned to
    /// </summary>            
    public string grp;

    /// <summary>
    /// C# string format string to format a double value pretty.
    /// Note: e.g. F4
    /// </summary>
    public string fmt;

    /// <summary>
    /// Unit to display.
    /// </summary>
    public string unit;

    /// <summary>
    /// Min and max values of the axes
    /// </summary>
    public double? xmin, ymin, xmax, ymax;

    /// <summary>
    /// Skip this plot in charts display
    /// </summary>
    public bool skip;

    /// <summary>
    /// Keep the plot on the same Y axis as the plot before
    /// </summary>
    public bool sameaxis;

    /// <summary>
    /// Plottables will be shown with ascending order
    /// </summary>
    public int order = -1;

    /// <summary>
    /// Width of plot line, size of its markers
    /// </summary>
    public double? linewidth, markersize;

    /// <summary>
    /// In order to display more than one bar plottable, set the 
    /// bar-width to 0.5 or 0.33 and the bar-offset to -0.5 .. +0.5
    /// </summary>
    public double? barwidth, barofs;

    /// <summary>
    /// Dimensions of the overall plot
    /// </summary>
    public double? height, width;

    /// <summary>
    /// For pie/bar-charts: initially display labels, values or percent 
    /// values.
    /// </summary>
    public bool labels, values, percent;

    /// <summary>
    /// Assign a predefined palette or style
    /// Palette: Aurora, Category10, Category20, ColorblindFriendly, Dark, 
    ///          DarkPastel, Frost, Microcharts, Nord, OneHalf, OneHalfDark, 
    ///          PolarNight, Redness, SnowStorm, Tsitsulin 
    /// Style: Black, Blue1, Blue2, Blue3, Burgundy, Control, Default, 
    ///        Gray1, Gray2, Light1, Light2, Monospace, Pink, Seaborn
    /// </summary>
    public string palette, style;

    public enum Type { None, Bars, Pie }

    /// <summary>
    /// Make a plot to be a bar or pie chart.
    /// Can be associated to TimeSeries or TimeSeriesVariable/ DataPoint
    /// </summary>
    public Type type;

    public enum Source { Timer, Event }

    /// <summary>
    /// Specify source for value updates.
    /// </summary>
    public Source src;

    /// <summary>
    /// Specifies the timer interval in milli-seconds. Minimum value 100ms.
    /// Applicable on: Submodel
    /// </summary>
    public int timer;

    /// <summary>
    /// Instead of displaying a list of plot items, display a set of tiles.
    /// Rows and columns can be assigned to the individual tiles.
    /// Applicable on: Submodel
    /// </summary>
    public bool tiles;

    /// <summary>
    /// Defines the zero-based row- and column position for tile based display.
    /// The span-settings allow stretching over multiple (>1) tiles.
    /// Applicable on: Properties
    /// </summary>
    public int? row, col, rowspan, colspan;
}
```

### Working sample for Plotting.Args

#### Submodel

The Submodel is attributed with some information:

Extension name: `Plotting.Args`

Extension value: 
`{ title: "Carbon monitoring", timer: 500, tiles: true }`

This will place the title as title of the right-hand panel of the
AASX Package Explorer. The timer for redisplaying the fixed-len
buffers is 500ms. Attributed data points will displayed as tiles
and not as a list.

#### Data points

Individual data points, most likely of type `Property`, are
attributed with:

Extension name: `Plotting.Args`

Extension value: 
`{ grp:1, src: "Event", title: "Phase voltages", fmt: "F0", 
   row: 6, col: 0, rowspan: 1, colspan:1, unit: "V", 
   linewidth: 1.0 }`

This plot-item will be displayed together with others in the 
first group.  The title of the joint display is "Phase voltages". 
It value updates come from the AAS event source instead 
of the timer. The number format for the lis/ tile display is "F0",
which means floating point with no numeric precision. Within the
tile display, its position is row 6 and column 0 (coordinates are
zero based). It is spanning exactly one tile. There seems no 
`ConceptDescription`available, therfore a unit in Volts is
attached. The linewidth of the chart is 1.0 pixels.

> Note: `style` and `palette` could be set for plot items but
> also globally for the Submodel.

> Note: The settings for `style` and `palette` seem not to 
> work, as of today.

#### Descriptions

As Submodels might use the `ConceptDescriptions` for the same
data point (example: "phase voltage L1, L2, L3"), it might be
reasonable to give individual explanations. This can be done
by giving the `description` attribute of the respective SME of
the data point. It is recommended to provide multi-language
descriptions.

### Working sample for TimeSeries.Args 

#### SMC for /TimeSeriesData/TimeSeries/1/0

The topmost SMC is attributed with some information:

Extension name: `TimeSeries.Args`

Extension value: 
`{ height: 400, palette: "Category10" }`

The plot area of this chart has a height of 400 pixel. During 
display, the height can be resized, as well. A color palette
is selected from the given enum names (see above).

> Note: The settings for `style` and `palette` seem not to 
> work, as of today.