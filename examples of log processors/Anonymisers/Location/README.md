# Simple Wi-Fi Logger
[![License](https://img.shields.io/badge/LICENSE-GPL%20v3.0-green?style=flat-square)](/LICENSE) 

## Examples : Anonymisers : Location
The following part of the script code should catch your attention:
```
example_my = '55.471106,52.471506' #Current location
example_need = '56.4825094497115,84.982061' #Needed location
```

Here, in the first line, separated by commas, you enter the coordinates of any point on your route (or just any point in the vicinity), and in the second line you need to enter the coordinates of the point to which you want to transfer your route relative to the previously specified point.

The script itself works quite simply: it calculates the delta between the desired point and the point that is already there, and adds this delta to all coordinates. In this way, the trajectory is not distorted, but simply transferred to another place.

P.S. By the way, this script was also used to process the log for example.

### Dots and commas
It is important to note that the log from the example uses a comma in the apparent decimal separator. 
This is a region-dependent parameter (depending on the settings of your phone), which can both help with parsing and interfere (not all programming languages take into account the country standard when parsing floating numbers).

So to work with coordinates, you may need to add either dots for commas or commas for dots.

As a developer of Simple Wi-Fi Logger, I don't consider this a disadvantage. Even in a sense, the opposite. This is a kind of "restraining" factor for thoughtless use of other people's scripts. An insignificant, but still a factor :)