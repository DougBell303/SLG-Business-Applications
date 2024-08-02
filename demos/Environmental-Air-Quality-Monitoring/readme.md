# Environmental Air Quality Monitoring
![AQM](https://i.imgur.com/ifQcQSQ.png)
This is a solution that showcases how State & Local Government's can monitor environmental air quality with:
- A **Raspberry Pi Pico W**, serving as an IoT device, that collects air quality data via an **ENS160** and **DHT-22** sensor
- A **Power Automate** flow that receives this data from the Raspberry Pi via an HTTP POST request trigger
- A **Dataverse** table that the Power Automate flow places the data into, using several *Formula columns*
- A **Power Apps** Model-Driven App to display collected Air Quality Reading Data
- A **Power BI** dashboard that reports air quality data and trends in a visual report, reading directly from Dataverse.

## Why is Environmental Air Quality Important to SLG?
- **Protect Public Health** - Poor air quality is a major environmental risk to human health, particularly for vulnerable populations such as children, older adults, and those with pre-existing medical conditions. Exposure to polluted air can lead to respiratory problems, cardiovascular disease, and even premature death. By tracking ambient air quality, you can identify areas of concern and take proactive measures to mitigate the health impacts of poor air quality.
- **Improve Environmental Sustainability** - Ambient air quality tracking is a critical component of environmental sustainability efforts. By monitoring and addressing air quality issues, you can reduce the environmental footprint of your community and promote a healthier, more sustainable environment for future generations.
- **Support Economic Develoment** - Clean air is essential for attracting businesses, tourists, and residents to your community. Areas with poor air quality can experience reduced economic growth, lower property values, and decreased quality of life. By demonstrating a commitment to air quality monitoring and improvement, you can enhance your community's reputation and competitiveness.
- **Meet Regulatory Requirements** - State and local governments are responsible for implementing federal air quality standards and regulations. Effective tracking and monitoring of ambient air quality conditions are critical to meeting these requirements and avoiding non-compliance penalties.

## Solution Gallery
The Raspberry Pi Pico W, DHT-22, and ENS160 sensor it its 3D-printed housing: 

![housing](https://i.imgur.com/KcKyxU2.jpeg)

Mounted outside, observing external conditions:

![mounted](https://i.imgur.com/xvwSLxR.png)

The IoT device sends sample data to Power Platform via HTTP POST request to a Power Automate workflow:

![Power Automate post](https://i.imgur.com/My3Qeka.png)

A Dataverse "Formula" Column is used to calculate the **Absolute Humidity** from two known values: *Relative Humidity* and *Temperature*.

![dataverse formula](https://i.imgur.com/e5NtGmY.png)

A model-driven Power App displays a simple yet effective interface for showing **Air Quality Reading** records received from the IoT device:

![md data](https://i.imgur.com/dRHDaOQ.png)

![md app form](https://i.imgur.com/Zja5WAf.png)

A Power BI dashboard is embedded within a Power Apps Canvas App, providing a capable dashboard.

*(photo coming soon)*

## Assets
We're providing the following assets so you can replicate this project as well:
- The Raspberry Pi IoT device lives neatly in a 3D-printed housing. You can find the source files (.STL) files of the housing and print yourself [on Thingiverse](https://www.thingiverse.com/thing:6655576).
- The source code (written in MicroPython) that runs on the Raspberry Pi Pico W can be found in the [`src` folder](./src/)
- The Power Platform solution (.zip file)
    - [Version 1.0.0.3](https://github.com/microsoft/SLG-Business-Applications/releases/download/9/AirQualityMonitoring_1_0_0_3.zip) - base solution (table, cloud flow, model-driven app)

## How to Calculate Absolute Humidity from Relative Humidity and Temperature
In this project, we calculate *absolute* humidity from the *relative humidity* and *temperature* readings our IoT device samples and transmits. Below is the equation for calculating absolute humidity. We use this equation in our Dataverse **Formula** column.

![formulas](https://i.imgur.com/1aRPvRh.png)

Temperature is in Celsius, Relative Humidity as a percentage.

You can express the formula above as a Dataverse Formula column like so:
```
(6.112 * (2.71828^((17.67*(('Temperature, Fahrenheit'-32)*(5/9)))/((('Temperature, Fahrenheit'-32)*(5/9))+243.5))) * 'Relative Humidity' * 2.1674) / (273.15 + (('Temperature, Fahrenheit'-32)*(5/9)))
```

The above formulas are from [here](https://carnotcycle.wordpress.com/2012/08/04/how-to-convert-relative-humidity-to-absolute-humidity/).

## Power BI Expression for Converting to EST
To convert the `createdon` field (stored in UTC time) to EST:

```
[createdon] - #duration(0, 4, 0, 0)
```

## Other Misc Resources
- All photos of this project: https://imgur.com/a/9LA9yLG

## Credit
Solution created by [Tim Hanewich](https://github.com/TimHanewich).