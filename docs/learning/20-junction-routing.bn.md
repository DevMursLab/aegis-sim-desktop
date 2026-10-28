# Lesson 20 — Junction থেকে নতুন পথ

**Tools → Road network demo** খোলো। গাড়িগুলো depot-এ থাকতেই inspector-এর **Use junction routing** চাপো। এরপর তিনবার Step দাও। Tick 1-এ dispatch, tick 2-এ road-01-এর ৩০০ মিটার, tick 3-এ বাকি ১৪৫ মিটার শেষ করে গাড়ি junction-এ দাঁড়াবে।

এবার `road-03` বেছে **Close road / 5 ticks** চাপো। পরের Step-এ B-তে যাওয়ার গাড়ি road-02 নেবে। Cargo ও destination B-ই থাকবে; শুধু পরের পথ বদলাবে। Event row বেছে junction path-এর reason পড়ো। ফেরার সময়ও depot পর্যন্ত নতুন খোলা পথ নেওয়া যায়।

আগের assigned-trip mode-এ পুরো নির্ধারিত পথের একটি road বন্ধ হলেই গাড়ি থামত। এই নতুন mode-এ গাড়ি যে road-এ ঢুকেছে সেটি শেষ করবে, road-টি মাঝপথে বন্ধ হলেও। পরের node-এ পৌঁছে বন্ধ road বাদ দিয়ে Dijkstra চালায়। কোনো খোলা পথ না থাকলে সেই node-এই cargo নিয়ে অপেক্ষা করে। Shelter-এর Close selected route আগের মতো outbound service থামায়।

প্রতি tick-এ একটি road-এর ভেতরে সর্বোচ্চ metersPerTick মিটার যায়। Node-এ পৌঁছে বেঁচে যাওয়া movement budget বাদ যায়; একই tick-এ পরের road শুরু হয় না। তাই এই mode-এ সময় `sum(ceil(edgeMeters / metersPerTick))` দিয়ে হয়, পুরো route একসঙ্গে ভাগ করে হয় না। Dispatch-এর tick আলাদা। এটি discrete training model; বাস্তব traffic বা গাড়ির সঠিক সময় দাবি নয়।

`RoadJourney` মনে রাখে শেষ node, target node, entered road, তার সঠিক দিকে সাজানো points ও বাকি মিটার। Map-এর Position সেখান থেকে বের হয়। `TicksRemaining` ও `Duration` এখানে বর্তমান edge-এর tick বোঝায়; node-এ দাঁড়ালে দুটোই শূন্য। Array-এর কপি বাইরে যায়, তাই UI থেকে engine state বদলানো যায় না।

EnableJunctionRouting একটি saved command। চালু করার সময় সবাই depot-এ থাকতে হবে। চলন্ত অবস্থায় পুরোনো mode থেকে বদলানো যায় না; নতুন run বা depot-এ ফেরার অপেক্ষা করো। একবার চালু হলে সেই run-এ বন্ধ করা যায় না। Reset নতুন run বানায় এবং mode বন্ধ করে। Replay থেকে mode বা road বদলানো যায় না। Preview, SQL ও report একই command history থেকে ফল বানায়।

পুরোনো engine 3.1 history নতুন engine 3.2-তেও একই checksum-এ চলে এবং আগের whole-trip waiting নিয়ম রাখে। নতুন mode আলাদা hash branch ব্যবহার করে। অনুশীলন: baseline, dispatch-time detour ও junction diversion-এর completion tick তুলনা করো। শুধু routing নয়, per-edge rounding-ও কেন সময় বদলায় তা লিখো।

কোড: `JunctionRouting.cs`, `RoadRouting.cs`, `FleetSession.cs`, `RunHistory.cs` ও `RunViewModel.cs`।
