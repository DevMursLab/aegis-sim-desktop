# Lesson 15 — জরুরি shelter আগে

Source app-এ **Tools → Priority + limited stock demo** চাপো। Stock ২০, তিন গাড়ি, প্রতিটি capacity ১০।

| Shelter | Demand | Priority |
|---|---:|---:|
| A | ২৫ | ০ |
| B | ৫ | ৫ |
| C | ১৮ | ৯ |

Priority ০–৯। বড় সংখ্যা আগে; এটি চাহিদা বা packet count নয়।

প্রথম Step-এ Vehicle-01 C-এর ১০ নেয়। Vehicle-02 C-এর বাকি ৮ নেয়। Vehicle-03 আর C বাছবে না—ওর সব চাহিদা cargo-তে reserved। সে B-এর ২ নেয়। Stock শূন্য।

Tick ৩-এ C-এর ১৮ আর B-এর ২ পৌঁছায়। Tick ৪-এ সব গাড়ি ফিরে **SupplyExhausted** হয়। A-এর ২৫ আর B-এর ৩ এখনো দরকার। আরও ২৮ stock দিলে tick ১২-তে সব delivery ও return শেষ হবে। UI-এর Restock +20 দুইবার দিলে অতিরিক্ত stock বাকি থাকবে; ঠিক ২৮ যোগ করার হিসাবটি automated check-এ করা হয়।

## বাছাইয়ের নিয়ম

1. Route খোলা এবং unreserved demand আছে—এমন shelter রাখো।
2. Priority বেশি হলে আগে নাও।
3. সমান priority হলে scenario-র আগের তালিকার ক্রম রাখো।
4. আগের গাড়িতে reserved cargo বাদ দিয়ে নতুন load হিসাব করো।

বন্ধ urgent route-এর জন্য অন্য খোলা route থামবে না। তবে গাড়ি রওনা দেওয়ার পর তার cargo অন্য shelter-এ ঘুরিয়ে দেওয়া হয় না। Priority scenario শুরু হওয়ার আগে ঠিক হয়; live run-এ priority বদলানোর command নেই। Editor-এ বদলে **নতুন run** শুরু করতে হবে।

## কেন ওই shelter বাছল?

Event table-এর dispatch row বেছে নাও। নিচে priority, unreserved demand, loaded packet, বাকি stock ও tie rule দেখাবে। এটি engine-এর নিজস্ব decision trace, AI-এর বানানো ব্যাখ্যা নয়। Replay ও per-vehicle CSV-তেও একই trace থাকে।

Editor-এর **Priority (fleet)** column-এ ১০ দিয়ে দেখো—validation আটকে দেবে। ৮ দিলে গ্রহণ করবে। Fleet mode বন্ধ করলে priority শূন্য করে পুরোনো dispatch নিয়মে নতুন scenario হয়। Schema 1/2-এ nonzero priority নেই।

সব priority শূন্য হলে আগের fleet-এর ফল অক্ষত। Engine 2.5 পুরোনো 2.0, 2.1 ও 2.4 run পড়তে পারে; নতুন priority run খুলতে নতুন source app লাগে। পুরোনো 2.2 ZIP-তে এই feature নেই।

নিজে করো: A-কে priority ৯ করে নতুন run চালাও। প্রথম ২০ packet কোথায় গেল? এবার A আর C দুজনকেই ৯ দাও—কে আগে পায় এবং কেন? শুধু priority দিলেই সব shelter সমান অপেক্ষার সময় পাবে কি না নিজের ভাষায় লেখো।
