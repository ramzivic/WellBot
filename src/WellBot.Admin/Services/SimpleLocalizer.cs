using System;
using System.Collections.Generic;
using System.Globalization;

namespace WellBot.Admin.Services;

public class SimpleLocalizer
{
    public string Get(string key)
    {
        var lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        if (_dictionary.TryGetValue(lang, out var langDict) && langDict.TryGetValue(key, out var val))
        {
            return val;
        }
        if (_dictionary.TryGetValue("fr", out var frDict) && frDict.TryGetValue(key, out var frVal))
        {
            return frVal;
        }
        return key;
    }

    public string this[string key] => Get(key);

    private static readonly Dictionary<string, Dictionary<string, string>> _dictionary = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "fr", new Dictionary<string, string>
            {
                { "DashboardTitle", "Tableau de bord WellBot" },
                { "ActiveMachines", "Machines Actives" },
                { "NotificationsDisplayed", "Notifications Affichées" },
                { "Acknowledged", "Remerciements" },
                { "EngagementRate", "Taux d'Engagement" },
                { "EngagementByType", "Engagement par type de notification" },
                { "Type", "Type" },
                { "Displayed", "Affichées" },
                { "Rate", "Taux" },
                { "RecentEvents", "Derniers événements" },
                { "Loading", "Chargement..." },
                { "Dashboard", "Tableau de bord" },
                { "HealthTips", "Conseils Santé" },
                { "Notifications", "Notifications" },
                { "AddTip", "Ajouter un conseil" },
                { "Configure", "Configurer" },
                { "Category", "Catégorie" },
                { "TitleFR", "Titre (FR)" },
                { "MessageFR", "Message (FR)" },
                { "TitleEN", "Titre (EN)" },
                { "MessageEN", "Message (EN)" },
                { "TitleAR", "Titre (AR)" },
                { "MessageAR", "Message (AR)" },
                { "Save", "Enregistrer" },
                { "Cancel", "Annuler" },
                { "DeleteConfirm", "Voulez-vous vraiment supprimer ce conseil ?" },
                { "Error", "Erreur" },
                { "Interval", "Intervalle (min)" },
                { "Active", "Actif" },
                { "Animation", "Animation" },
                { "Title", "Titre" },
                { "Message", "Message" },
                { "PleaseLogin", "Voulez-vous vous connecter" },
                { "InvalidCredentials", "Identifiant ou mot de passe incorrect." },
                { "AccessDenied", "Accès refusé. Le compte client desktop ne peut pas accéder à l'interface d'administration." },
                { "Username", "Nom d'utilisateur" },
                { "Password", "Mot de passe" },
                { "Login", "Se connecter" },
                { "Logout", "Déconnexion" },
                { "LastXDays", "Derniers {0} jours" }
            }
        },
        {
            "en", new Dictionary<string, string>
            {
                { "DashboardTitle", "WellBot Dashboard" },
                { "ActiveMachines", "Active Machines" },
                { "NotificationsDisplayed", "Displayed Notifications" },
                { "Acknowledged", "Acknowledged" },
                { "EngagementRate", "Engagement Rate" },
                { "EngagementByType", "Engagement by Notification Type" },
                { "Type", "Type" },
                { "Displayed", "Displayed" },
                { "Rate", "Rate" },
                { "RecentEvents", "Recent Events" },
                { "Loading", "Loading..." },
                { "Dashboard", "Dashboard" },
                { "HealthTips", "Health Tips" },
                { "Notifications", "Notifications" },
                { "AddTip", "Add Tip" },
                { "Configure", "Configure" },
                { "Category", "Category" },
                { "TitleFR", "Title (FR)" },
                { "MessageFR", "Message (FR)" },
                { "TitleEN", "Title (EN)" },
                { "MessageEN", "Message (EN)" },
                { "TitleAR", "Title (AR)" },
                { "MessageAR", "Message (AR)" },
                { "Save", "Save" },
                { "Cancel", "Cancel" },
                { "DeleteConfirm", "Are you sure you want to delete this tip?" },
                { "Error", "Error" },
                { "Interval", "Interval (min)" },
                { "Active", "Active" },
                { "Animation", "Animation" },
                { "Title", "Title" },
                { "Message", "Message" },
                { "PleaseLogin", "Please log in" },
                { "InvalidCredentials", "Invalid username or password." },
                { "AccessDenied", "Access denied. The desktop client account cannot access the administration interface." },
                { "Username", "Username" },
                { "Password", "Password" },
                { "Login", "Log in" },
                { "Logout", "Log out" },
                { "LastXDays", "Last {0} days" }
            }
        },
        {
            "ar", new Dictionary<string, string>
            {
                { "DashboardTitle", "لوحة تحكم WellBot" },
                { "ActiveMachines", "الأجهزة النشطة" },
                { "NotificationsDisplayed", "الإشعارات المعروضة" },
                { "Acknowledged", "تم الإقرار" },
                { "EngagementRate", "معدل التفاعل" },
                { "EngagementByType", "التفاعل حسب نوع الإشعار" },
                { "Type", "النوع" },
                { "Displayed", "معروضة" },
                { "Rate", "المعدل" },
                { "RecentEvents", "الأحداث الأخيرة" },
                { "Loading", "جاري التحميل..." },
                { "Dashboard", "لوحة التحكم" },
                { "HealthTips", "نصائح صحية" },
                { "Notifications", "الإشعارات" },
                { "AddTip", "إضافة نصيحة" },
                { "Configure", "إعداد" },
                { "Category", "الفئة" },
                { "TitleFR", "العنوان (فرنسي)" },
                { "MessageFR", "الرسالة (فرنسي)" },
                { "TitleEN", "العنوان (إنجليزي)" },
                { "MessageEN", "الرسالة (إنجليزي)" },
                { "TitleAR", "العنوان (عربي)" },
                { "MessageAR", "الرسالة (عربي)" },
                { "Save", "حفظ" },
                { "Cancel", "إلغاء" },
                { "DeleteConfirm", "هل أنت متأكد أنك تريد حذف هذه النصيحة؟" },
                { "Error", "خطأ" },
                { "Interval", "الفاصل الزمني (دقيقة)" },
                { "Active", "نشط" },
                { "Animation", "حركة" },
                { "Title", "العنوان" },
                { "Message", "الرسالة" },
                { "PleaseLogin", "الرجاء تسجيل الدخول" },
                { "InvalidCredentials", "اسم المستخدم أو كلمة المرور غير صحيحة." },
                { "AccessDenied", "تم رفض الوصول. لا يمكن لحساب عميل سطح المكتب الوصول إلى واجهة الإدارة." },
                { "Username", "اسم المستخدم" },
                { "Password", "كلمة المرور" },
                { "Login", "تسجيل الدخول" },
                { "Logout", "تسجيل خروج" },
                { "LastXDays", "آخر {0} أيام" }
            }
        }
    };
}
