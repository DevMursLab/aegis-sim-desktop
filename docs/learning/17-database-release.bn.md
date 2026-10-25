# Lesson 17 — পুরোনো data রেখে নতুন release

নতুন app বানালেই পুরোনো saved run মুছে ফেলা যায় না। তাই database-এর version রাখা হয়েছে: SQLite `user_version = 1` এবং `SchemaMigrations` table।

পুরোনো database খুললে প্রথমে SQLite-এর backup API দিয়ে একই অবস্থার backup হয়। তারপর transaction-এর মধ্যে প্রয়োজনীয় table ও পুরোনো event data তৈরি হয়। সব ঠিক হলে commit; কোনো history corrupt হলে rollback। Backup-এর নাম app জানায়। আগে থেকেই version 1 থাকলে আবার backup/upgrade হয় না। App-এর চেয়ে নতুন version বা অপরিচিত database হলে বদলানো হয় না।

কোড দেখো: `DatabaseSchema.Prepare` পরীক্ষা ও backup করে। `RunRepository` transaction শুরু করে। `DatabaseSchema.Complete` সফল upgrade-এর version লেখে। এই release-এর নির্দিষ্ট upgrade পরীক্ষা করা হয়েছে; ভবিষ্যতের প্রতিটি schema change-এর জন্য আবার migration লাগবে।

Release দেওয়ার আগে চালাও:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

প্রথম script build, ১৫৭টি console check এবং বাস্তব WPF flow চালায়। দ্বিতীয়টি runtime-সহ Windows ZIP বানায়। ZIP extract করে `Start-Local.cmd` চালালে আলাদা SDK লাগে না। Optional `Install.ps1` নিজের user-এর Programs folder-এ copy করে। SHA-256 দিয়ে download-এর bytes ঠিক আছে কি না মেলানো যায়।

অভ্যাস: নতুন খালি data folder-এ app খোলো, একটি run শেষ করো, বন্ধ করে আবার খুলে replay দেখো। তারপর PDF/HTML/CSV export করো। নিজের আসল saved data মুছে পরীক্ষা কোরো না।

Interview-তে বলার অনুশীলন: cargo reservation কেন দরকার, shortest path কীভাবে স্থির হয়, transaction কেন লাগে, replay hash কী যাচাই করে, আর এই simulator-এর সীমা কী। শুধু feature-এর নাম নয়—একটি উদাহরণ দিয়ে বোঝাও।
