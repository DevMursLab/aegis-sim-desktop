# Lesson 14 — একসঙ্গে একাধিক গাড়ি

Source থেকে desktop খুলে **Tools → Three-vehicle demo** চাপো। তিন গাড়ি একই গুদামের stock ব্যবহার করবে। প্রতিটি গাড়ির capacity ১০, যেতে ২ tick, ফিরতে ১ tick। সব shelter মিলে চাহিদা ৪৮।

## Step দিয়ে মিলিয়ে দেখো

| Tick | কী হবে |
|---|---|
| ১ | A-এর জন্য তিন গাড়িতে ১০ + ১০ + ৫ ওঠে। Cargo ২৫, depot-এ ২৩; delivered ০। |
| ২ | তিন গাড়ি পথে আছে। |
| ৩ | A-তে মোট ২৫ পৌঁছায়। গাড়ি ফেরার phase-এ যায়। |
| ৪ | তিন গাড়ি depot-এ ফেরে। |
| ৫ | এক গাড়িতে B-এর ৫, অন্য দুই গাড়িতে C-এর ১০ ও ৮ ওঠে। |
| ৭ | বাকি ২৩ পৌঁছায়; মোট delivered ৪৮। |
| ৮ | সব গাড়ি ফিরে এলে Completed হয়। |

একই stock ও travel settings-এ এক গাড়ির ছয় trip লাগে: ৬ × ৪ = ২৪ tick। তিন গাড়ি দুই wave-এ কাজ করে: ২ × ৪ = ৮ tick। এটি এই model-এর হিসাব, বাস্তব গাড়ির speed-এর দাবি নয়।

## একই চাহিদা দুবার বরাদ্দ হচ্ছে না কেন?

A-এর চাহিদা ২৫। প্রথম গাড়িতে ১০ দিলে তা এখনো delivered নয়, কিন্তু **reserved cargo**। দ্বিতীয় গাড়ি হিসাব করবে ২৫ − ১০ = ১৫। দ্বিতীয় গাড়িতে ১০ ওঠার পর তৃতীয় গাড়ি পাবে মাত্র ৫। এভাবে অপ্রয়োজনীয় ৩০ প্যাকেট পাঠানো হবে না।

প্রতি tick-এ আগে আগের যাত্রায় থাকা গাড়িগুলো এগোয়। তারপর tick-এর শুরুতে depot-এ থাকা গাড়িগুলো স্থির ID ক্রমে load নেয়। এই tick-এ ফিরেছে এমন গাড়ি পরের tick-এর আগে আবার ছাড়ে না। এটি তিনটি আলাদা thread নয়; একটি নির্দিষ্ট scheduler সব গাড়ির state বদলায়।

## Editor ও history

**Scenario editor**-এ Multiple vehicles চালিয়ে ১–৮টি গাড়ি দাও। Capacity প্রতিটি গাড়ির জন্য প্রযোজ্য; stock সবার জন্য একটাই। Shelter বাছাইয়ের নিয়ম এখনো তালিকার ক্রম; priority dispatch পরের কাজ।

Event table-এ vehicle ID দেখবে। Database-এ প্রতি tick-এর মধ্যে সব গাড়ির action থাকে। Replay, completion preview ও multiplayer একই fleet ফের তৈরি করে। Fleet report-এ অতিরিক্ত `-vehicles.csv` থাকে। Outbound/return metrics হলো **vehicle-ticks**; তিন গাড়ি এক tick চললে সেখানে ৩ যোগ হবে।

পুরোনো schema 1/2-এর hash ও নিয়ম অক্ষত। Schema 3-এর জন্য engine 2.4 লাগে; আগের app দিয়ে নতুন run খোলা যাবে না। সর্বশেষ প্রকাশিত 2.2 ZIP-তে fleet নেই—source থেকে চালাও। Map-এ কাছাকাছি গাড়ির icon আলাদা দেখাতে সামান্য screen offset দেওয়া হয়; engine-এর অবস্থান বদলায় না।

নিজে করো: stock ৩০ করে চালাও। Tick ৮-এ কেন SupplyExhausted হয় দেখো। Restock +20 দিলে tick ১২-তে শেষ হয়, stock ২ থাকে। তারপর fleet size ১, ২, ৪ করে ফল তুলনা করো।
