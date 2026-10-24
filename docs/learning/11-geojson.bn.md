# Lesson 11 — ফাইল থেকে map layer

আগের simulation map আঁকা ছিল local x/y দিয়ে। এবার **Tools → Offline GeoJSON layers** খুলে `assets/maps/synthetic-region.geojson` বেছে নাও।

GeoJSON-এ shape রাখা যায়। আমাদের viewer তিনটা shape বোঝে:

- `Point`: depot বা কেন্দ্রের একটি অবস্থান।
- `LineString`: কয়েকটি অবস্থান জুড়ে রাস্তা বা নদী।
- `Polygon`: বন্ধ একটি এলাকার সীমানা।

ডান পাশের তালিকা থেকে feature বাছলে নাম, ID, shape আর position count দেখাবে। Points, Lines, Areas বন্ধ করে layer আলাদা করে দেখো। Mouse wheel দিয়ে zoom, drag করে pan, Home দিয়ে fit করো।

## Coordinates-এর পার্থক্য

GeoJSON-এর প্রতিটি position এখানে `[longitude, latitude]`। এগুলো degree। Simulation-এর `[480,180]` স্থানীয় coordinate-কে GPS ভেবে import করা যাবে না।

`GeoJsonLayer` ফাইল যাচাই করে, তারপর display projection দিয়ে screen-এর উপযোগী coordinate বানায়। উত্তর ওপরের দিকে থাকে। এই projection দূরত্ব মাপার জন্য নয়। সর্বোচ্চ দুই degree বিস্তৃত ছোট region নেওয়া হয়।

**এই viewer-এর layer simulation-এর route বা সময় বদলায় না।** Simulation আর geographic view-এর coordinate system আলাদা। Exercise history-তেও layer সংরক্ষিত হয় না। Sample-এর সব feature কাল্পনিক।

## ভুল হলে কী হয়

ভাঙা JSON, duplicate ID, degree-এর বাইরে coordinate, খোলা/self-crossing polygon বা বড় file গ্রহণ করা হয় না। আগের ভালো layer পর্দায় থাকে।

নিজে করো: sample-এর কপি বানাও। একটি point-এর নাম পাল্টাও, তারপর খুলে দেখো। এবার একই ID দুই জায়গায় দাও—কেন error আসে বুঝে লেখো। পরে polygon-এর শেষ position সরাও; প্রথম position-এর সঙ্গে শেষটি না মিললে কেন area বন্ধ হয় না বোঝাও।
