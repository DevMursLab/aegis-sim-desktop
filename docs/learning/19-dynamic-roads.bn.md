# Lesson 19 — রাস্তা বন্ধ হলে বিকল্প পথ

**Tools → Road network demo** খোলো। Road picker থেকে `road-01` বেছে **Close road / 5 ticks** চাপো। এরপর Step দাও। আগে B ও C-তে যাওয়ার পথ depot থেকে road-01 নিত। এখন বন্ধ রাস্তা বাদ দিয়ে `road-99` দিয়ে বিকল্প পথ নেওয়া হবে। Map-এ বন্ধ রাস্তা লাল, গাড়ি বিকল্প বাঁক ধরে চলে। Dispatch row বেছে পথ, মিটার ও tick দেখো।

প্রতিটি road-এর দুই প্রান্ত ও ধনাত্মক দৈর্ঘ্য আছে। `RoadRouting.Find` Dijkstra চালায়, বন্ধ road বাদ দেয়। কম মোট দৈর্ঘ্যের path নেয়; সমান distance হলে node ID ও road ID দিয়ে স্থির নিয়মে বেছে নেয়। Import-এর মূল route বদলায় না; প্রতিটি dispatch নিজের route ধরে রাখে। দূরত্বকে metersPerTick দিয়ে ভাগ করে ওপরের পূর্ণসংখ্যায় travel duration হয়।

Close command tick বাড়ায় না। Tick 0-তে 5 tick বন্ধ করলে tick 1 থেকে 5 পর্যন্ত road ব্যবহার করা যায় না; tick 6-এ খোলে। একই road আবার বন্ধ করলে আগের expiry কমে না। Depot-এর `road-01` ও `road-99` একসঙ্গে বন্ধ করে দেখো: গাড়ি অপেক্ষা করবে, stock থেকে cargo কাটবে না। পথ খুললে dispatch হবে। উঁচু priority-র shelter অগম্য হলে পৌঁছানো যায় এমন পরের shelter বেছে নেয়।

গাড়ি dispatch হয়ে গেলে তার পুরো নির্ধারিত path-এর কোনো road বন্ধ থাকলে যাত্রা একই অবস্থানে থামে। গাড়ি যদি সেই edge পার হয়েও থাকে, এই সরল মডেল পুরো trip-কে থামায়। ফেরার ক্ষেত্রেও একই নিয়ম; ফেরার path outbound-এর একই পথ। চলন্ত অবস্থায় অন্য পথে ঘুরিয়ে দেওয়ার বা বাস্তব traffic simulation-এর দাবি নেই। **Close selected route** আগের মতো শুধু shelter-এর outbound service বন্ধ করে; **Close road** shared network-এ প্রযোজ্য।

Reset করে একটি নতুন run বানাও। Step দিয়ে কিছুদূর যাওয়ার পর road বন্ধ করে দেখো। Cargo ও map position অপরিবর্তিত থাকবে যতক্ষণ অপেক্ষা চলছে। Saved run খুলে replay করলে road incident, নির্বাচিত route ও checksum মিলে যাবে; replay থেকে road বন্ধ করা যায় না। Completion preview incident-এর অবশিষ্ট সময় ধরে simulation-এর কপি চালায়। HTML-এ road incident, vehicle CSV-তে dispatch path থাকে।

নতুন run-এর engine 3.1.0। পুরোনো 3.0 history একই hash-এ পড়া যায়, কিন্তু তাতে নতুন road command ঢোকানো হয় না। Code: `RoadRouting.cs`, `FleetSession.cs`, `RunHistory.cs`, `RunViewModel.cs`। অনুশীলন: একই stock ও demand রেখে road-01 বন্ধ করলে completion tick কেন বদলায়, নিজের ভাষায় বোঝাও।
