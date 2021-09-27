with open('log_0_.csv', 'r', encoding="utf-8") as file:
    first = file.readline()
    lines = file.readlines()
 

example_my = '55.471106,52.471506' #Current location
example_need = '56.4825094497115,84.982061' #Needed location
correction_latid = float(example_need.split(',')[0]) - float(example_my.split(',')[0])
correction_longtid = float(example_need.split(',')[1]) - float(example_my.split(',')[1])

for item in lines:
    coord = item.split(';')[11].strip()
    if coord != 'null':
        latid = float(coord.replace(',', '.'))
        latid += correction_latid
        lines = [w.replace(coord, str(latid).replace('.', ',')) for w in lines]
        
    coord = item.split(';')[12].strip()
    if coord != 'null':
        latid = float(coord.replace(',', '.'))
        latid += correction_longtid
        lines = [w.replace(coord, str(latid).replace('.', ',')) for w in lines]

    coord = item.split(';')[18].strip()
    if coord != 'null':
        latid = float(coord.replace(',', '.'))
        latid += correction_latid
        lines = [w.replace(coord, str(latid).replace('.', ',')) for w in lines]

    coord = item.split(';')[19].strip()
    if coord != 'null':
        latid = float(coord.replace(',', '.'))
        latid += correction_longtid
        lines = [w.replace(coord, str(latid).replace('.', ',')) for w in lines]

with open('log_1_.csv', 'w', encoding="utf-8") as file:
    file.write(first)
    for item in lines:
        file.write("%s" % item)
