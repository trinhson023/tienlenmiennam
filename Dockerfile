# Sử dụng Node 14 để tương thích hoàn toàn với node-sass 4.14.1 và react-scripts 4
FROM node:14-buster-slim

WORKDIR /app

# Copy file cấu hình package để tận dụng Docker layer cache
COPY package*.json ./

# Cài đặt dependencies (cần cả devDependencies để chạy react-scripts build)
RUN npm install

# Copy toàn bộ mã nguồn vào container
COPY . .

# Patch boardgame.io deduplication
RUN node scripts/patch-boardgame.js

# Build React frontend (tạo thư mục ./build)
ENV CI=false
RUN npm run build

# Expose port 8000 cho cả giao diện web và WebSocket game
EXPOSE 8000
ENV PORT=8000
ENV NODE_ENV=production

# Khởi động game server (node -r esm server.js)
CMD ["npm", "run", "server"]
