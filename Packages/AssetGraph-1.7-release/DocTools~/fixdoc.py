import os

srcfile = 'DocTools~/assetgraph_from_gdoc.md'
pnglist = 'DocTools~/order.txt'

dstfile = 'Documentation~/assetgraph.md'
num = 1

if os.path.exists(dstfile):
	os.remove(dstfile)

with open(srcfile) as f:
	doc = f.read()
	f.close()
	with open(pnglist) as fpng:
		while True:
			pnglist = fpng.readline()
			if not pnglist:
				break
			pnglist = pnglist.strip()
			keyword = "image{0}.png".format(num)
			print(keyword + " => " + pnglist)
			doc = doc.replace(keyword, pnglist.replace("image","__temp__"))
			num+=1


	doc = doc.replace("__temp__", "image")

	with open(dstfile, mode='w') as fw:
		fw.write(doc)
		fw.close()
