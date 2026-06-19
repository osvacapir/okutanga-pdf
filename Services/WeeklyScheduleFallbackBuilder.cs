using OlondongeApp.Models.Dtos;

namespace OlondongeApp.Services;

/// <summary>
/// Gera um horário semanal plausível quando a API não devolve slots, usando só os nomes das disciplinas em cache.
/// </summary>
public static class WeeklyScheduleFallbackBuilder
{
    private const int LessonMinutes = 45;
    private const int IntervalBetweenLessonsMinutes = 5;
    /// <summary>Intervalo maior entre a 3.ª e a 4.ª aula (recreio curto típico).</summary>
    private const int LongBreakAfterThirdLessonMinutes = 15;
    private const int SlotsPerSchoolDay = 6;

    private static readonly (int Weekday, string WeekdayName)[] Weekdays =
    {
        (1, "Segunda-feira"),
        (2, "Terça-feira"),
        (3, "Quarta-feira"),
        (4, "Quinta-feira"),
        (5, "Sexta-feira"),
    };

    /// <summary>
    /// Um dia: 6 tempos de 45 min; 5 min entre tempos, exceto 15 min após a 3.ª aula (antes da 4.ª).
    /// <paramref name="firstLessonStartMinutesFromMidnight"/> = minutos desde 00:00 (ex.: 7*60+15 = 07:15).
    /// </summary>
    private static (string Start, string End)[] BuildSixSlotDay(int firstLessonStartMinutesFromMidnight)
    {
        var slots = new (string Start, string End)[SlotsPerSchoolDay];
        var cursor = firstLessonStartMinutesFromMidnight;
        for (var i = 0; i < SlotsPerSchoolDay; i++)
        {
            var start = cursor;
            var end = cursor + LessonMinutes;
            slots[i] = (ToHHmm(start), ToHHmm(end));
            if (i < SlotsPerSchoolDay - 1)
            {
                var gapAfterThisLesson = i == 2 ? LongBreakAfterThirdLessonMinutes : IntervalBetweenLessonsMinutes;
                cursor = end + gapAfterThisLesson;
            }
        }

        return slots;
    }

    private static string ToHHmm(int totalMinutesFromMidnight)
    {
        var h = totalMinutesFromMidnight / 60;
        var m = totalMinutesFromMidnight % 60;

        return $"{h:D2}:{m:D2}";
    }

    /// <summary>
    /// Grelha 2ª–6ª com <see cref="SlotsPerSchoolDay"/> tempos por dia; preenche cada célula ciclindo as disciplinas
    /// (não há matriz real na API — evita o antigo round-robin que dava só ~N/5 aulas por dia).
    /// </summary>
    /// <param name="firstLessonStartMinutesFromMidnight">Início do 1.º tempo (ex. manhã 435 = 07:15, tarde 780 = 13:00).</param>
    public static List<WeeklyScheduleSlotDto> Build(IReadOnlyList<string> disciplineNames, int firstLessonStartMinutesFromMidnight)
    {
        if (disciplineNames.Count == 0)
        {
            return new List<WeeklyScheduleSlotDto>();
        }

        var slotTimes = BuildSixSlotDay(firstLessonStartMinutesFromMidnight);

        var ordered = disciplineNames
            .Where(static n => !string.IsNullOrWhiteSpace(n))
            .Select(static n => n.Trim())
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(static n => n, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var nDisc = ordered.Count;
        var totalCells = Weekdays.Length * SlotsPerSchoolDay;
        var list = new List<WeeklyScheduleSlotDto>(totalCells);
        var slotId = 1;
        for (var dayIdx = 0; dayIdx < Weekdays.Length; dayIdx++)
        {
            var day = Weekdays[dayIdx];
            for (var slotIdx = 0; slotIdx < SlotsPerSchoolDay; slotIdx++)
            {
                var time = slotTimes[slotIdx];
                var discipline = ordered[(dayIdx * SlotsPerSchoolDay + slotIdx) % nDisc];
                list.Add(new WeeklyScheduleSlotDto
                {
                    Weekday = day.Weekday,
                    WeekdayName = day.WeekdayName,
                    HorarioSlotId = slotId++,
                    SlotOrder = slotIdx + 1,
                    SlotName = $"Tempo {slotIdx + 1}",
                    StartTime = time.Start,
                    EndTime = time.End,
                    DisciplinaName = discipline,
                    DisciplinaAbreviatura = null,
                    TeacherName = null,
                });
            }
        }

        return list;
    }
}
