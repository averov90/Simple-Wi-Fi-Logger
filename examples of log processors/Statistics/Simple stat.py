from datetime import datetime

with open('log_0_.csv', 'r', encoding="utf-8") as file:
  file.readline() #Read(skip) first line (with annotations)
  lines = file.readlines()

measurement_count = 0
prew_datetime = datetime.now()

net_list = []
max_list = []
max_time = prew_datetime
max_location = []

avr_netcount = 0

closest_ap_distance = 9999
closest_ap_time = prew_datetime
closest_ap_name = ""

for item in lines:
  datetime_object = datetime.strptime(item.split(';')[0].strip(), '%Y.%m.%d %H:%M:%S.%f')
  
  if prew_datetime != datetime_object:
      measurement_count+=1
      prew_datetime = datetime_object
      
      net_list = []

  frequrency = item.split(';')[6].strip()
  if (int(frequrency) > 5000):
      continue;

  if float(item.split(';')[3].strip().replace(',', '.')) < closest_ap_distance:
      closest_ap_distance = float(item.split(';')[3].strip().replace(',', '.'))
      closest_ap_time = datetime_object
      closest_ap_name = item.split(';')[7].strip()

  avr_netcount += 1

  net_list.append(item.split(';')[3].strip().replace(',', '.')+"m >" + item.split(';')[7].strip())
  if (len(net_list) > len(max_list)):
      max_time = datetime_object
      max_location = [w.strip().replace(',', '.') for w in item.split(';')[11:]]
      max_list = net_list.copy()


def GetLocationAvg(locationA, locationB):
  loc = 0
  count = 0
  if (locationA != 'null'):
      loc += float(locationA)
      count += 1
      
  if (locationB != 'null'):
      loc += float(locationB)
      count += 1

  if (count != 0):
      return str(loc / count)
  else:
      return 'null'

print("Measurements total: " + str(measurement_count))
print("Average visible networks count (2.4 only): " + str(avr_netcount / float(measurement_count)))
print("Scan result with most of ap found (2.4 only): " + str(len(max_list)))
print("Time: " + str(max_time) + " Location: " + GetLocationAvg(max_location[0], max_location[7]) + ' ' + GetLocationAvg(max_location[1], max_location[8]))
for item in max_list:
  print(item)
print()
print("Closest AP (2.4) name: " + closest_ap_name)
print("Closest AP (2.4) distance: " + str(closest_ap_distance))
print("Closest AP (2.4) time: " + str(closest_ap_time))
