# Simple Wi-Fi Logger
[![License](https://img.shields.io/badge/LICENSE-GPL%20v3.0-green?style=flat-square)](/LICENSE) 

## Examples : Statistics
The parser given in the current folder produces the following example result:

```
Measurements total: 87
Average visible networks count (2.4 only): 10.885057471264368
Scan result with most of ap found (2.4 only): 26
Time: 2021-09-03 13:46:04.700000 Location: 56.48560439468265 84.98240581987504
43.2725m >Goblin
122.2066m >TP-LINK_3B8C
171.574m >FTTXB725C1
175.116m >TP-LINK_2853
197.298m >RT-WiFi-F73B
278.6908m >obi7
310.7637m >
310.7637m >Allan
345.8327m >FTTX6708AA
345.8327m >Keenetic-C7B0
344.4251m >TP-Link_AB46
343.7256m >MTS_0143A3958
392.8469m >MERCUSYS_2EC5
432.725m >RT-WiFi-5932
430.9745m >MTSRouter-DBAAA3
441.6952m >RT-WiFi-D620
441.6952m >MTSRouter-087E8F
440.7815m >Maga
438.9653m >Shaggi
432.725m >TP-Link_F5AE
432.725m >MegaHome
493.544m >TP-LINK_53DB
491.5146m >PINK
551.4885m >TP-LINK_F2
548.1078m >RT-WiFi_6882
700.0397m >MTS_Router_091247-1186

Closest AP (2.4) name: AP_014
Closest AP (2.4) distance: 0.4416952
Closest AP (2.4) time: 2021-09-03 13:46:59.400000
```

As you can see from the code, this parser-analyzer compiles some basic log statistics: the number of scans during the logging period;
average number of 2.4 GHz access points visible from the operator's smartphone; the maximum number of points in one scan, the time of this scan, the coordinates and the list itself (on the left is the distance to the AP based on the signal strength, on the right is the name of the AP);
the name of the point closest to the operator's 2.4 GHz route, as well as the distance to it and the fixation time.

Of course, for a real task, the above parser-analyzer is unlikely to be useful, but using its example it is easy to write a parser with the necessary functionality.