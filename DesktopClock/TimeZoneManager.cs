using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

// MIT License
// Copyright (c) 2026 lin3310 (林楷庭)
// SPDX-License-Identifier: MIT

namespace DesktopClock;

public static class TimeZoneManager
{
	public static async Task<List<string>> GetTimeZonesAsync()
	{
		try
		{
			using HttpClient client = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(3.0)
			};
			return JsonSerializer.Deserialize<List<string>>(await client.GetStringAsync("http://worldtimeapi.org/api/timezone")) ?? GetSystemTimeZones();
		}
		catch
		{
			return GetSystemTimeZones();
		}
	}

	public static List<string> GetSystemTimeZones()
	{
		return (from tz in TimeZoneInfo.GetSystemTimeZones()
			select tz.Id into id
			orderby id
			select id).ToList();
	}

	public static DateTime GetTimeInZone(string timeZoneId)
	{
		if (timeZoneId == "Local")
		{
			return DateTime.Now;
		}
		try
		{
			TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
			return TimeZoneInfo.ConvertTime(DateTime.Now, tz);
		}
		catch
		{
			return DateTime.Now;
		}
	}
}
