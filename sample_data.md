# Sample Test Data for India Map Plotter

Create an Excel file named `test_data.xlsx` with the following data:

## Sheet 1: Valid Data

| Latitude | Longitude | SetupType |
|----------|-----------|-----------|
| 28.6139  | 77.2090   | 2S        |
| 19.0760  | 72.8777   | 3S        |
| 13.0827  | 80.2707   | SS        |
| 22.5726  | 88.3639   | 2s        |
| 12.9716  | 77.5946   | 3s        |
| 17.3850  | 78.4867   | ss        |
| 26.8467  | 80.9462   | 2S        |
| 23.0225  | 72.5714   | 3S        |
| 21.1702  | 72.8311   | SS        |
| 18.5204  | 73.8567   | 2S        |

## Major Indian Cities Included:
- New Delhi (2S)
- Mumbai (3S)
- Chennai (SS)
- Kolkata (2s - lowercase test)
- Bangalore (3s - lowercase test)
- Hyderabad (ss - lowercase test)
- Lucknow (2S)
- Ahmedabad (3S)
- Surat (SS)
- Pune (2S)

##To test error handling, add rows with:
- Invalid latitude: 95.0, 77.0, 2S
- Invalid longitude: 28.0, 200.0, 3S
- Invalid SetupType: 28.0, 77.0, 4S
- Empty cells: , 77.0, 2S

