export const carData: Record<string, string[]> = {
  Toyota: [
    "Corolla",
    "Corolla Cross",
    "Camry",
    "Yaris",
    "Rush",
    "Fortuner",
    "Land Cruiser",
  ],

  Nissan: [
    "Sunny",
    "Sentra",
    "Altima",
    "Qashqai",
    "Juke",
    "X-Trail",
  ],

  Hyundai: [
    "Accent",
    "Elantra",
    "Elantra AD",
    "Elantra CN7",
    "Tucson",
    "Creta",
    "Verna",
    "Santa Fe",
  ],

  Kia: [
    "Cerato",
    "Cerato K3",
    "Sportage",
    "Sorento",
    "Picanto",
    "Rio",
    "Seltos",
  ],

  Chevrolet: ["Optra", "Aveo", "Malibu", "Captiva"],

  Mazda: ["Mazda 2", "Mazda 3", "CX-3", "CX-5"],

  Mitsubishi: ["Lancer", "Xpander", "Outlander"],

  Suzuki: ["Swift", "Ciaz", "Ertiga", "Vitara", "Alto"],

  BMW: ["116", "118", "316", "318", "320", "X1", "X3", "X5"],

  "Mercedes-Benz": ["A180", "C180", "C200", "E200", "GLA", "GLC"],

  Audi: ["A3", "A4", "A6", "Q2", "Q3", "Q5"],

  Volkswagen: ["Golf", "Passat", "Tiguan"],

  Skoda: ["Octavia", "Superb", "Kodiaq", "Kamiq"],

  Peugeot: ["301", "508", "2008", "3008", "5008"],

  Renault: ["Logan", "Sandero", "Megane", "Duster"],

  Fiat: ["Tipo", "500"],

  Opel: ["Astra", "Corsa", "Grandland"],

  Chery: ["Arrizo 5", "Tiggo 2", "Tiggo 3", "Tiggo 7", "Tiggo 8"],

  BYD: ["F3", "L3"],

  MG: ["MG 5", "MG 6", "ZS", "HS", "RX5"],

  Geely: ["Emgrand", "Coolray", "GX3"],

  Haval: ["H6", "Jolion"],

  Changan: ["Alsvin", "Eado", "CS35", "CS55"],

  Jetour: ["X70", "X90"],

  Exeed: ["TXL", "VX"],

  GAC: ["GS3", "GS4"],

  Forthing: ["T5 Evo"],

  Ford: ["Focus", "Fusion", "EcoSport", "Kuga"],

  Jeep: ["Renegade", "Compass", "Grand Cherokee"],

  "Land Rover": ["Range Rover", "Evoque", "Discovery"],

  Volvo: ["S60", "XC40", "XC60"],
}

// 🔥 derive brands automatically (مفيش bugs)
export const carBrands = Object.keys(carData)